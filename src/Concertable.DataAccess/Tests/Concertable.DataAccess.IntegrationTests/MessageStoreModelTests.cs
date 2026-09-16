using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Concertable.DataAccess.IntegrationTests;

public sealed class MessageStoreModelTests : IDisposable
{
    private readonly SqliteConnection connection;

    public MessageStoreModelTests()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();
    }

    [Fact]
    public void OnModelCreating_EveryMessageStoreColumn_NamesNoProviderSpecificType()
    {
        using var context = this.CreateContext();

        var declared = MessageStoreProperties(context)
            .Select(property => property.GetColumnType())
            .Where(columnType => columnType is not null)
            .Where(columnType => !string.Equals(columnType, "TEXT", StringComparison.Ordinal))
            .Where(columnType => !string.Equals(columnType, "INTEGER", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(declared);
    }

    [Fact]
    public void OnModelCreating_BoundedMessageStoreColumns_KeepTheLengthTheRealTableHas()
    {
        using var context = this.CreateContext();

        Assert.Equal(450, Property(context, typeof(OutboxMessageEntity), nameof(OutboxMessageEntity.MessageType)).GetMaxLength());
        Assert.Equal(450, Property(context, typeof(OutboxMessageEntity), nameof(OutboxMessageEntity.CorrelationId)).GetMaxLength());
        Assert.Equal(450, Property(context, typeof(InboxMessageEntity), nameof(InboxMessageEntity.MessageType)).GetMaxLength());
        Assert.Equal(256, Property(context, typeof(InboxMessageEntity), nameof(InboxMessageEntity.ConsumerName)).GetMaxLength());
    }

    [Fact]
    public void OnModelCreating_OutboxDispatchQuery_IsIndexedAsTheOwningConfigurationDeclaresIt()
    {
        using var context = this.CreateContext();

        var index = Assert.Single(context.Model.FindEntityType(typeof(OutboxMessageEntity))!.GetIndexes());

        Assert.Equal(
            [nameof(OutboxMessageEntity.Status), nameof(OutboxMessageEntity.OccurredAtUtc)],
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void GenerateCreateScript_ConsumerOwningTheseEntities_EmitsNoMessagingTableOrIndex()
    {
        using var context = this.CreateContext();

        var script = context.Database.GenerateCreateScript();

        Assert.DoesNotContain("Outbox", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Inbox", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"Entities\"", script, StringComparison.Ordinal);
    }

    public void Dispose() => this.connection.Dispose();

    private ConsumerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ConsumerDbContext>().UseSqlite(this.connection).Options);

    private static IEnumerable<IProperty> MessageStoreProperties(DbContext context) =>
        new[] { typeof(OutboxMessageEntity), typeof(InboxMessageEntity) }
            .Select(type => context.Model.FindEntityType(type)!)
            .SelectMany(entityType => entityType.GetProperties());

    private static IProperty Property(DbContext context, Type entityType, string propertyName) =>
        context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

    private sealed class ConsumerDbContext(DbContextOptions<ConsumerDbContext> options) : DbContextBase(options)
    {
        public DbSet<ConsumerEntity> Entities => Set<ConsumerEntity>();
    }

    private sealed class ConsumerEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
