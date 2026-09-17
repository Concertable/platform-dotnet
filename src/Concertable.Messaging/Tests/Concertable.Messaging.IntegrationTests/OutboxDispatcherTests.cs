using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.Messaging.IntegrationTests;

[Collection(MessageStoreCollection.Name)]
public sealed class OutboxDispatcherTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Base = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly MessageStoreFixture fixture;

    public OutboxDispatcherTests(MessageStoreFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region DrainOnceAsync

    [Fact]
    public async Task DrainOnce_APendingEvent_PublishesItAndMarksItDispatched()
    {
        var transport = new Mock<IBusTransport>();
        await SeedAsync(1);

        await DrainAsync(transport.Object, Base.AddSeconds(10));

        transport.Verify(t => t.PublishAsync(
            It.IsAny<FakeIntegrationEvent>(),
            It.IsAny<MessageEnvelope>(),
            It.IsAny<CancellationToken>()), Times.Once);

        await using var probe = fixture.CreateOutboxContext();
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Dispatched, stored.Status);
        Assert.Equal(Base.AddSeconds(10), stored.DispatchedAtUtc);
    }

    [Fact]
    public async Task DrainOnce_ATransportThatRefuses_RecordsTheFailureAndKeepsTheRowPending()
    {
        var transport = new Mock<IBusTransport>();
        transport
            .Setup(t => t.PublishAsync(It.IsAny<FakeIntegrationEvent>(), It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker down"));
        await SeedAsync(1);

        await DrainAsync(transport.Object, Base);

        await using var probe = fixture.CreateOutboxContext();
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Pending, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("broker down", stored.LastError);
    }

    [Fact]
    public async Task DrainOnce_OneRowOfTheBatchReclaimedMidDrain_StillCommitsTheRest()
    {
        var transport = new Mock<IBusTransport>();
        var reclaimed = new TaskCompletionSource();
        transport
            .Setup(t => t.PublishAsync(It.IsAny<FakeIntegrationEvent>(), It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (reclaimed.Task.IsCompleted) return;
                await ReclaimOneRowAsync();
                reclaimed.SetResult();
            });
        await SeedAsync(3);

        await DrainAsync(transport.Object, Base.AddSeconds(10));

        await using var probe = fixture.CreateOutboxContext();
        var stored = await probe.Set<OutboxMessageEntity>().ToListAsync();
        Assert.Equal(2, stored.Count(row => row.Status == OutboxStatus.Dispatched));
    }

    #endregion

    private async Task SeedAsync(int count)
    {
        await using var context = fixture.CreateOutboxContext();
        for (var i = 0; i < count; i++)
        {
            var payload = new MessageSerializer().Serialize(new FakeIntegrationEvent(Guid.NewGuid(), "concert", i)).ToString();
            context.Add(OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base.AddSeconds(i), MessageKind.Event));
        }
        await context.SaveChangesAsync();
    }

    private async Task ReclaimOneRowAsync()
    {
        await using var context = fixture.CreateOutboxContext();
        await context.Database.ExecuteSqlRawAsync(
            $"""
            UPDATE "{Schema.Name}"."{Schema.Tables.Outbox}" SET "NextRetryAtUtc" = now()
            WHERE "Id" = (
                SELECT "Id" FROM "{Schema.Name}"."{Schema.Tables.Outbox}"
                WHERE "Status" = {(int)OutboxStatus.Dispatching}
                ORDER BY "OccurredAtUtc" LIMIT 1)
            """);
    }

    private async Task DrainAsync(IBusTransport transport, DateTimeOffset now)
    {
        var registry = new MessageTypeRegistry();
        registry.SubscribeTo<FakeIntegrationEvent>();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));
        services.AddSingleton(transport);
        services.AddSingleton(registry);
        services.AddLogging();
        services.AddOutbox(
            options => options.UseNpgsql(
                fixture.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(MessageStoreFixture.OutboxHistory, Schema.Name)),
            outbox => outbox.MaxAttempts = 3);

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<OutboxOptions>>(),
            provider.GetRequiredService<TimeProvider>(),
            NullLogger<OutboxDispatcher>.Instance);

        await dispatcher.DrainOnceAsync(CancellationToken.None);
    }
}
