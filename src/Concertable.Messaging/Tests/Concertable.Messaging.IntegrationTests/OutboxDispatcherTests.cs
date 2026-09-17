using System.Reflection;
using Concertable.Messaging.Application.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure;
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
    private const string OutboxHistory = "__EFMigrationsHistory_Outbox";

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
        await SeedAsync();

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
        await SeedAsync();

        await DrainAsync(transport.Object, Base);

        await using var probe = fixture.CreateOutboxContext();
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Pending, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("broker down", stored.LastError);
    }

    #endregion

    private async Task SeedAsync()
    {
        var payload = new MessageSerializer().Serialize(new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1)).ToString();
        await using var context = fixture.CreateOutboxContext();
        context.Add(OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base, MessageKind.Event));
        await context.SaveChangesAsync();
    }

    private async Task DrainAsync(IBusTransport transport, DateTimeOffset now)
    {
        var registry = new MessageTypeRegistry();
        registry.SubscribeTo<FakeIntegrationEvent>();

        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(options => options.UseNpgsql(
            fixture.ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable(OutboxHistory, Schema.Name)));
        services.AddSingleton<IOptions<OutboxOptions>>(Options.Create(new OutboxOptions()));
        services.AddScoped<IOutboxReader, OutboxReader>();
        services.AddSingleton(transport);
        services.AddSingleton(registry);
        services.AddScoped<EventDispatcher>();
        services.AddScoped<CommandDispatcher>();
        services.AddScoped<IMessageDispatchResolver, MessageDispatchResolver>();
        services.AddSingleton(new MessageSerializer());
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(now));

        await using var provider = services.BuildServiceProvider();
        var dispatcher = new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { MaxAttempts = 3 }),
            new FakeTimeProvider(now),
            NullLogger<OutboxDispatcher>.Instance);

        var drain = typeof(OutboxDispatcher).GetMethod(
            "DrainOnceAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)drain.Invoke(dispatcher, [CancellationToken.None])!;
    }
}
