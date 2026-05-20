using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.Messaging.UnitTests;

public class OutboxDispatcherTests
{
    private static readonly DateTimeOffset Base = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    private static OutboxDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<OutboxDbContext>().UseInMemoryDatabase(dbName).Options,
            Options.Create(new OutboxOptions()));

    private static ServiceProvider BuildProvider(string dbName, IBusTransport transport, MessageTypeRegistry registry)
    {
        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<IOptions<OutboxOptions>>(Options.Create(new OutboxOptions()));
        services.AddScoped<IOutboxReader, OutboxReader>();
        services.AddSingleton(transport);
        services.AddSingleton(registry);
        services.AddSingleton(new MessageSerializer());
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DispatchOnce_PublishesPendingEventThroughTransportAndMarksDispatched()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var transport = new Mock<IBusTransport>();
        var registry = new MessageTypeRegistry();
        registry.SubscribeTo<FakeIntegrationEvent>();

        await using (var seed = NewContext(dbName))
        {
            var payload = new MessageSerializer().Serialize(new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1)).ToString();
            seed.Add(OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base, MessageKind.Event));
            await seed.SaveChangesAsync();
        }

        var provider = BuildProvider(dbName, transport.Object, registry);
        var dispatcher = new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { MaxAttempts = 3 }),
            new FakeTimeProvider(Base.AddSeconds(10)),
            NullLogger<OutboxDispatcher>.Instance);

        // Act
        await InvokeDrainAsync(dispatcher);

        // Assert
        transport.Verify(t => t.PublishAsync(
            It.IsAny<FakeIntegrationEvent>(),
            It.IsAny<MessageEnvelope>(),
            It.IsAny<CancellationToken>()), Times.Once);

        await using var probe = NewContext(dbName);
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Dispatched, stored.Status);
        Assert.Equal(Base.AddSeconds(10), stored.DispatchedAtUtc);
    }

    [Fact]
    public async Task DispatchOnce_WhenTransportFails_RecordsFailureAndKeepsPending()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var transport = new Mock<IBusTransport>();
        transport.Setup(t => t.PublishAsync(It.IsAny<FakeIntegrationEvent>(), It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker down"));
        var registry = new MessageTypeRegistry();
        registry.SubscribeTo<FakeIntegrationEvent>();

        await using (var seed = NewContext(dbName))
        {
            var payload = new MessageSerializer().Serialize(new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1)).ToString();
            seed.Add(OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base, MessageKind.Event));
            await seed.SaveChangesAsync();
        }

        var provider = BuildProvider(dbName, transport.Object, registry);
        var dispatcher = new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { MaxAttempts = 3 }),
            new FakeTimeProvider(Base),
            NullLogger<OutboxDispatcher>.Instance);

        // Act
        await InvokeDrainAsync(dispatcher);

        // Assert
        await using var probe = NewContext(dbName);
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Pending, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("broker down", stored.LastError);
    }

    private static Task InvokeDrainAsync(OutboxDispatcher dispatcher)
    {
        var method = typeof(OutboxDispatcher).GetMethod(
            "DrainOnceAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(dispatcher, [CancellationToken.None])!;
    }
}
