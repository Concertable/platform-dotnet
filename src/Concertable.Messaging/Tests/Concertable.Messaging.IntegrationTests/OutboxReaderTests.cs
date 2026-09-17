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
    public async Task GetPendingAsync_PendingAndDispatchedRows_ClaimsOnlyThePendingOnesOldestFirst()
    {
        var older = Message("{\"i\":1}", Base);
        var newer = Message("{\"i\":2}", Base.AddMinutes(5));
        var dispatched = Message("{\"i\":3}", Base.AddMinutes(1));
        dispatched.MarkDispatched(Base.AddMinutes(2));
        await SeedAsync(older, newer, dispatched);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context).GetPendingAsync(batchSize: 50);

        Assert.Equal([older.Id, newer.Id], pending.Select(row => row.Id));
    }

    [Fact]
    public async Task GetPendingAsync_MoreRowsThanTheBatch_ClaimsOnlyTheBatch()
    {
        await SeedAsync([.. Enumerable.Range(0, 5).Select(i => Message($"{{\"i\":{i}}}", Base.AddSeconds(i)))]);

        await using var context = fixture.CreateOutboxContext();
        var pending = await NewReader(context).GetPendingAsync(batchSize: 2);

        Assert.Equal(2, pending.Count);
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
    public async Task GetPendingAsync_TwoWorkersAtOnce_SplitTheBatchWithNoRowClaimedTwice()
    {
        await SeedAsync([.. Enumerable.Range(0, 20).Select(i => Message($"{{\"i\":{i}}}", Base.AddSeconds(i)))]);

        await using var first = fixture.CreateOutboxContext();
        await using var second = fixture.CreateOutboxContext();
        var claims = await Task.WhenAll(
            NewReader(first).GetPendingAsync(batchSize: 20),
            NewReader(second).GetPendingAsync(batchSize: 20));

        var ids = claims.SelectMany(claim => claim.Select(row => row.Id)).ToList();
        Assert.Equal(20, ids.Count);
        Assert.Equal(20, ids.Distinct().Count());
    }

    [Fact]
    public async Task GetPendingAsync_AfterALeaseExpires_ReclaimsTheAbandonedRow()
    {
        await SeedAsync(Message("{}", Base));

        await using var abandoning = fixture.CreateOutboxContext();
        await NewReader(abandoning, Base).GetPendingAsync(batchSize: 50);

        await using var reclaiming = fixture.CreateOutboxContext();
        var reclaimed = await NewReader(reclaiming, Base.Add(Lease).AddSeconds(1)).GetPendingAsync(batchSize: 50);

        Assert.Single(reclaimed);
    }

    #endregion

    private OutboxReader NewReader(OutboxDbContext context, DateTimeOffset? now = null) =>
        new(context, Options.Create(new OutboxOptions { LeaseDuration = Lease }), new FakeTimeProvider(now ?? Base));

    private static OutboxMessageEntity Message(string payload, DateTimeOffset occurredAt) =>
        OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, occurredAt, MessageKind.Event);

    private async Task SeedAsync(params OutboxMessageEntity[] messages)
    {
        await using var context = fixture.CreateOutboxContext();
        context.AddRange(messages);
        await context.SaveChangesAsync();
    }
}
