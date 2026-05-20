using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.UnitTests;

public class OutboxReaderTests
{
    private static readonly DateTimeOffset Base = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    private static OutboxDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<OutboxDbContext>().UseInMemoryDatabase(dbName).Options,
            Options.Create(new OutboxOptions()));

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyPendingOrderedByOccurredAt()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = NewContext(dbName);
        var older = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{\"i\":1}", Base, MessageKind.Event);
        var newer = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{\"i\":2}", Base.AddMinutes(5), MessageKind.Event);
        var dispatched = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{\"i\":3}", Base.AddMinutes(1), MessageKind.Event);
        dispatched.MarkDispatched(Base.AddMinutes(2));
        context.AddRange(older, newer, dispatched);
        await context.SaveChangesAsync();
        var reader = new OutboxReader(context);

        // Act
        var pending = await reader.GetPendingAsync(batchSize: 50);

        // Assert
        Assert.Equal(2, pending.Count);
        Assert.Equal(older.Id, pending[0].Id);
        Assert.Equal(newer.Id, pending[1].Id);
    }

    [Fact]
    public async Task GetPendingAsync_RespectsBatchSize()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = NewContext(dbName);
        for (var i = 0; i < 5; i++)
            context.Add(OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), $"{{\"i\":{i}}}", Base.AddSeconds(i), MessageKind.Event));
        await context.SaveChangesAsync();
        var reader = new OutboxReader(context);

        // Act
        var pending = await reader.GetPendingAsync(batchSize: 2);

        // Assert
        Assert.Equal(2, pending.Count);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsStatusChanges()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        await using var context = NewContext(dbName);
        var row = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Base, MessageKind.Event);
        context.Add(row);
        await context.SaveChangesAsync();
        var reader = new OutboxReader(context);

        // Act
        row.MarkDispatched(Base.AddMinutes(1));
        await reader.SaveChangesAsync();

        // Assert
        await using var probe = NewContext(dbName);
        var stored = Assert.Single(await probe.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Equal(OutboxStatus.Dispatched, stored.Status);
    }
}
