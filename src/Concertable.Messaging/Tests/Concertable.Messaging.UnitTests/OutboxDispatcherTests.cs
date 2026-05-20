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

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessageEntity>(b =>
            {
                b.HasKey(m => m.Id);
                b.Property(m => m.MessageType).IsRequired();
                b.Property(m => m.Payload).IsRequired();
            });
        }
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
            var store = new OutboxStore<TestDbContext>(seed);
            var payload = new MessageSerializer().Serialize(new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1)).ToString();
            var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base, MessageKind.Event);
            await store.AddAsync(row);
            await store.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IOutboxStore, OutboxStore<TestDbContext>>();
        services.AddSingleton(transport.Object);
        services.AddSingleton(registry);
        services.AddSingleton(new MessageSerializer());
        var provider = services.BuildServiceProvider();

        var dispatcher = new OutboxDispatcher<TestDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { MaxAttempts = 3 }),
            new FakeTimeProvider(Base.AddSeconds(10)),
            NullLogger<OutboxDispatcher<TestDbContext>>.Instance);

        // Act
        await InvokeDrainAsync(dispatcher);

        // Assert
        transport.Verify(t => t.PublishAsync(
            It.IsAny<FakeIntegrationEvent>(),
            It.IsAny<MessageEnvelope>(),
            It.IsAny<CancellationToken>()), Times.Once);

        await using var probe = NewContext(dbName);
        var rows = await probe.Set<OutboxMessageEntity>().ToListAsync();
        var stored = Assert.Single(rows);
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
            var store = new OutboxStore<TestDbContext>(seed);
            var payload = new MessageSerializer().Serialize(new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1)).ToString();
            var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), payload, Base, MessageKind.Event);
            await store.AddAsync(row);
            await store.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IOutboxStore, OutboxStore<TestDbContext>>();
        services.AddSingleton(transport.Object);
        services.AddSingleton(registry);
        services.AddSingleton(new MessageSerializer());
        var provider = services.BuildServiceProvider();

        var dispatcher = new OutboxDispatcher<TestDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { MaxAttempts = 3 }),
            new FakeTimeProvider(Base),
            NullLogger<OutboxDispatcher<TestDbContext>>.Instance);

        // Act
        await InvokeDrainAsync(dispatcher);

        // Assert
        await using var probe = NewContext(dbName);
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Pending, stored.Status);
        Assert.Equal(1, stored.Attempts);
        Assert.Equal("broker down", stored.LastError);
    }

    private static TestDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(dbName).Options);

    private static Task InvokeDrainAsync(OutboxDispatcher<TestDbContext> dispatcher)
    {
        var method = typeof(OutboxDispatcher<TestDbContext>).GetMethod(
            "DrainOnceAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(dispatcher, [CancellationToken.None])!;
    }
}
