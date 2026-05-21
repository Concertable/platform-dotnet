using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.UnitTests;

public class OutboxWriterTests
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

    private static TestDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task AddAsync_AddsToCurrentContext_NotPersistedUntilSaveChanges()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = NewContext(dbName);
        var writer = new OutboxWriter(new DbContextAccessor { Context = context });
        var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Base, MessageKind.Event);

        // Act
        await writer.AddAsync(row);

        // Assert
        await using (var beforeSave = NewContext(dbName))
            Assert.Empty(await beforeSave.Set<OutboxMessageEntity>().ToListAsync());

        await context.SaveChangesAsync();

        await using var afterSave = NewContext(dbName);
        Assert.Single(await afterSave.Set<OutboxMessageEntity>().ToListAsync());
    }

    [Fact]
    public async Task AddAsync_Throws_WhenNoContextIsCurrent()
    {
        // Arrange
        var writer = new OutboxWriter(new DbContextAccessor());
        var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Base, MessageKind.Event);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.AddAsync(row));
    }
}
