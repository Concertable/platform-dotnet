using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Concertable.Messaging.IntegrationTests;

[Collection(MessageStoreCollection.Name)]
public sealed class OutboxReaderTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Base = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private readonly MessageStoreFixture fixture;

    public OutboxReaderTests(MessageStoreFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region GetPendingAsync

    [Fact]
    public async Task GetPendingAsync_PendingAndDispatchedRows_ClaimsOnlyThePendingOnes()
    {
        var older = Message("{\"i\":1}", Base);
        var newer = Message("{\"i\":2}", Base.AddMinutes(5));
        var dispatched = Message("{\"i\":3}", Base.AddMinutes(1));
        dispatched.MarkDispatched(Base.AddMinutes(2));
        await SeedAsync(older, newer, dispatched);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context).GetPendingAsync(batchSize: 50);

        Assert.Equal(new[] { older.Id, newer.Id }.Order(), pending.Select(row => row.Id).Order());
    }

    [Fact]
    public async Task GetPendingAsync_MoreRowsThanTheBatch_ClaimsTheOldestOnesUpToIt()
    {
        var seeded = Enumerable.Range(0, 5)
            .Select(i => Message($"{{\"i\":{i}}}", Base.AddSeconds(i)))
            .ToArray();
        await SeedAsync([.. seeded.Reverse()]);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context).GetPendingAsync(batchSize: 2);

        Assert.Equal(
            seeded.Take(2).Select(row => row.Id).Order(),
            pending.Select(row => row.Id).Order());
    }

    [Fact]
    public async Task GetPendingAsync_ADeadLetteredRow_IsNeverClaimedAgain()
    {
        var exhausted = Message("{}", Base);
        exhausted.RecordFailure("gave up", maxAttempts: 1, Base);
        var ready = Message("{}", Base.AddSeconds(1));
        await SeedAsync(exhausted, ready);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context, Base.AddDays(1)).GetPendingAsync(batchSize: 50);

        Assert.Equal(OutboxStatus.DeadLettered, exhausted.Status);
        Assert.Equal(ready.Id, Assert.Single(pending).Id);
    }

    [Fact]
    public async Task GetPendingAsync_RetryStillInTheFuture_LeavesTheRow()
    {
        var ready = Message("{\"i\":1}", Base);
        var deferred = Message("{\"i\":2}", Base);
        deferred.RecordFailure("err", maxAttempts: 10, Base);
        await SeedAsync(ready, deferred);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context, Base).GetPendingAsync(batchSize: 50);

        Assert.Equal(ready.Id, Assert.Single(pending).Id);
    }

    [Fact]
    public async Task GetPendingAsync_RetryWindowPassed_ClaimsTheRow()
    {
        var deferred = Message("{}", Base);
        deferred.RecordFailure("err", maxAttempts: 10, Base);
        await SeedAsync(deferred);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context, Base.AddSeconds(2)).GetPendingAsync(batchSize: 50);

        Assert.Single(pending);
    }

    [Fact]
    public async Task GetPendingAsync_ClaimedRow_IsMarkedDispatchingAndLeasedInTheStore()
    {
        await SeedAsync(Message("{}", Base));

        await using (var claiming = fixture.CreateOutboxContext())
            await NewReader(claiming).GetPendingAsync(batchSize: 50);

        await using var probe = fixture.CreateOutboxContext();
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Dispatching, stored.Status);
        Assert.Equal(Base.Add(Lease), stored.NextRetryAtUtc);
    }

    [Fact]
    public async Task GetPendingAsync_ASecondWorkerWhileTheLeaseHolds_ClaimsNothing()
    {
        await SeedAsync(Message("{}", Base));

        await using var first = fixture.CreateOutboxContext();
        await using var second = fixture.CreateOutboxContext();
        var claimed = await NewReader(first).GetPendingAsync(batchSize: 50);
        var contested = await NewReader(second).GetPendingAsync(batchSize: 50);

        Assert.Single(claimed);
        Assert.Empty(contested);
    }

    [Fact]
    public async Task GetPendingAsync_WhileAnotherWorkersClaimIsStillOpen_SkipsItsRowsInsteadOfWaiting()
    {
        await SeedAsync([.. Enumerable.Range(0, 20).Select(i => Message($"{{\"i\":{i}}}", Base.AddSeconds(i)))]);

        await using var holding = fixture.CreateOutboxContext();
        await using var holdingTransaction = await holding.Database.BeginTransactionAsync();
        var held = await NewReader(holding).GetPendingAsync(batchSize: 10);

        await using var skipping = fixture.CreateOutboxContext();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var skipped = await NewReader(skipping).GetPendingAsync(batchSize: 10, deadline.Token);

        await holdingTransaction.CommitAsync();

        Assert.Equal(10, held.Count);
        Assert.Equal(10, skipped.Count);
        Assert.Empty(held.Select(row => row.Id).Intersect(skipped.Select(row => row.Id)));
    }

    [Fact]
    public async Task GetPendingAsync_AtTheLeaseDeadline_ReclaimsTheAbandonedRow() =>
        Assert.Single(await ReclaimAtAsync(Base.Add(Lease)));

    [Fact]
    public async Task GetPendingAsync_AMomentBeforeTheLeaseDeadline_LeavesTheRowToItsHolder() =>
        Assert.Empty(await ReclaimAtAsync(Base.Add(Lease).AddSeconds(-1)));

    #endregion

    private OutboxReader NewReader(OutboxDbContext context, DateTimeOffset? now = null) =>
        new(context, Options.Create(new OutboxOptions { LeaseDuration = Lease }), new FakeTimeProvider(now ?? Base));

    private static OutboxMessageEntity Message(string payload, DateTimeOffset occurredAt) =>
        OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, occurredAt, MessageKind.Event);

    private async Task<IReadOnlyList<OutboxMessageEntity>> ReclaimAtAsync(DateTimeOffset now)
    {
        await SeedAsync(Message("{}", Base));

        await using var abandoning = fixture.CreateOutboxContext();
        await NewReader(abandoning, Base).GetPendingAsync(batchSize: 50);

        await using var reclaiming = fixture.CreateOutboxContext();
        return await NewReader(reclaiming, now).GetPendingAsync(batchSize: 50);
    }

    private async Task SeedAsync(params OutboxMessageEntity[] messages)
    {
        await using var context = fixture.CreateOutboxContext();
        context.AddRange(messages);
        await context.SaveChangesAsync();
    }
}
