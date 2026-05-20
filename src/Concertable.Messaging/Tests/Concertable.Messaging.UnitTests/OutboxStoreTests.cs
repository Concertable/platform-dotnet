using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.UnitTests;

public class OutboxStoreTests
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

    private static TestDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task AddAsync_DoesNotPersistUntilSaveChanges()
    {
        // Arrange
        await using var context = NewContext();
        var store = new OutboxStore<TestDbContext>(context);
        var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Base, MessageKind.Event);

        // Act
        await store.AddAsync(row);

        // Assert
        await using var probe = NewContext();
        Assert.Empty(await probe.Set<OutboxMessageEntity>().ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsAddedRow()
    {
        // Arrange
        await using var context = NewContext();
        var store = new OutboxStore<TestDbContext>(context);
        var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Base, MessageKind.Event);
        await store.AddAsync(row);

        // Act
        await store.SaveChangesAsync();

        // Assert
        Assert.NotNull(await context.Set<OutboxMessageEntity>().FindAsync(row.Id));
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingOrderedByOccurredAt()
    {
        // Arrange
        await using var context = NewContext();
        var store = new OutboxStore<TestDbContext>(context);
        var older = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{\"i\":1}", Base, MessageKind.Event);
        var newer = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{\"i\":2}", Base.AddMinutes(5), MessageKind.Event);
        var dispatched = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{\"i\":3}", Base.AddMinutes(1), MessageKind.Event);
        dispatched.MarkDispatched(Base.AddMinutes(2));
        await store.AddAsync(older);
        await store.AddAsync(newer);
        await store.AddAsync(dispatched);
        await store.SaveChangesAsync();

        // Act
        var pending = await store.GetPendingAsync(batchSize: 50);

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.Equal(older.Id, pending[0].Id);
        Assert.Equal(newer.Id, pending[1].Id);
    }

    [Fact]
    public async Task GetPendingAsync_RespectsBatchSize()
    {
        // Arrange
        await using var context = NewContext();
        var store = new OutboxStore<TestDbContext>(context);
        for (var i = 0; i < 5; i++)
        {
            var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), $"{{\"i\":{i}}}", Base.AddSeconds(i), MessageKind.Event);
            await store.AddAsync(row);
        }
        await store.SaveChangesAsync();

        // Act
        var pending = await store.GetPendingAsync(batchSize: 2);

        // Assert
        Assert.Equal(2, pending.Count);
    }
}
