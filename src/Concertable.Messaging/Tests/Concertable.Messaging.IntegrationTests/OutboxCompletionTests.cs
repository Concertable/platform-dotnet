using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Concertable.Messaging.IntegrationTests;

[Collection(MessageStoreCollection.Name)]
public sealed class OutboxCompletionTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Base = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private readonly MessageStoreFixture fixture;

    public OutboxCompletionTests(MessageStoreFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_TheWorkerStillHoldingItsLease_PersistsTheDispatch()
    {
        await SeedAsync(Message("{}"));

        await using var context = fixture.CreateOutboxContext();
        var claimed = Assert.Single(await NewReader(context, Base).GetPendingAsync(batchSize: 50));
        claimed.MarkDispatched(Base.AddSeconds(1));
        await context.SaveChangesAsync();

        await using var probe = fixture.CreateOutboxContext();
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Dispatched, stored.Status);
        Assert.Null(stored.NextRetryAtUtc);
    }

    [Fact]
    public async Task SaveChangesAsync_AWorkerWhoseLeaseAnotherAlreadyTook_LosesItsDispatch()
    {
        await SeedAsync(Message("{}"));

        await using var stale = fixture.CreateOutboxContext();
        var claimed = Assert.Single(await NewReader(stale, Base).GetPendingAsync(batchSize: 50));

        await using (var reclaiming = fixture.CreateOutboxContext())
            await NewReader(reclaiming, Base.Add(Lease).AddSeconds(1)).GetPendingAsync(batchSize: 50);

        claimed.MarkDispatched(Base.AddSeconds(1));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_AWorkerWhoseLeaseAnotherAlreadyTook_LeavesTheRowToTheHolder()
    {
        await SeedAsync(Message("{}"));

        await using var stale = fixture.CreateOutboxContext();
        var claimed = Assert.Single(await NewReader(stale, Base).GetPendingAsync(batchSize: 50));

        await using (var reclaiming = fixture.CreateOutboxContext())
            await NewReader(reclaiming, Base.Add(Lease).AddSeconds(1)).GetPendingAsync(batchSize: 50);

        claimed.MarkDispatched(Base.AddSeconds(1));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());

        await using var probe = fixture.CreateOutboxContext();
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Dispatching, stored.Status);
        Assert.Equal(Base.Add(Lease).AddSeconds(1).Add(Lease), stored.NextRetryAtUtc);
    }

    #endregion

    private OutboxReader NewReader(OutboxDbContext context, DateTimeOffset now) =>
        new(context, Options.Create(new OutboxOptions { LeaseDuration = Lease }), new FakeTimeProvider(now));

    private static OutboxMessageEntity Message(string payload) =>
        OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base, MessageKind.Event);

    private async Task SeedAsync(params OutboxMessageEntity[] messages)
    {
        await using var context = fixture.CreateOutboxContext();
        context.AddRange(messages);
        await context.SaveChangesAsync();
    }
}
