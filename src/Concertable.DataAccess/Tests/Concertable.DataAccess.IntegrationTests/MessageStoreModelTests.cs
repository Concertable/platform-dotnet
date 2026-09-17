using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

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
        using var context = CreateContext();

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
        using var context = CreateContext();

        Assert.Equal(450, Property(context, typeof(OutboxMessageEntity), nameof(OutboxMessageEntity.MessageType)).GetMaxLength());
        Assert.Equal(450, Property(context, typeof(OutboxMessageEntity), nameof(OutboxMessageEntity.CorrelationId)).GetMaxLength());
        Assert.Equal(450, Property(context, typeof(InboxMessageEntity), nameof(InboxMessageEntity.MessageType)).GetMaxLength());
        Assert.Equal(256, Property(context, typeof(InboxMessageEntity), nameof(InboxMessageEntity.ConsumerName)).GetMaxLength());
    }

    [Fact]
    public void OnModelCreating_OutboxDispatchQuery_IsIndexedAsTheOwningConfigurationDeclaresIt()
    {
        using var context = CreateContext();

        var index = Assert.Single(context.Model.FindEntityType(typeof(OutboxMessageEntity))!.GetIndexes());

        Assert.Equal(
            [nameof(OutboxMessageEntity.Status), nameof(OutboxMessageEntity.OccurredAtUtc)],
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void GenerateCreateScript_ConsumerOwningTheseEntities_EmitsNoMessagingTableOrIndex()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();

        Assert.DoesNotContain("Outbox", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Inbox", script, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"Entities\"", script, StringComparison.Ordinal);
    }

    [Fact]
    public void OnModelCreating_TheDefaultOptions_MapsBothMessageStoresAtTheMessagingSchema()
    {
        using var context = CreateContext();

        Assert.Equal(Schema.Name, context.Model.FindEntityType(typeof(OutboxMessageEntity))!.GetSchema());
        Assert.Equal(Schema.Name, context.Model.FindEntityType(typeof(InboxMessageEntity))!.GetSchema());
    }

    [Fact]
    public void OnModelCreating_AHostThatMovedItsOutbox_MapsItWhereThatHostPollsIt()
    {
        // EF keys its model cache on the context type, so a second schema needs a second type, not a
        // second options instance.
        using var context = new MovedOutboxDbContext(
            new DbContextOptionsBuilder<MovedOutboxDbContext>().UseSqlite(connection).Options,
            Options.Create(new OutboxOptions { SchemaName = "tickets" }));

        Assert.Equal("tickets", context.Model.FindEntityType(typeof(OutboxMessageEntity))!.GetSchema());
        Assert.Equal(Schema.Name, context.Model.FindEntityType(typeof(InboxMessageEntity))!.GetSchema());
    }

    public void Dispose() => connection.Dispose();

    private ConsumerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ConsumerDbContext>().UseSqlite(connection).Options,
            Options.Create(new OutboxOptions()));

    private static IEnumerable<IProperty> MessageStoreProperties(DbContext context) =>
        new[] { typeof(OutboxMessageEntity), typeof(InboxMessageEntity) }
            .Select(type => context.Model.FindEntityType(type)!)
            .SelectMany(entityType => entityType.GetProperties());

    private static IProperty Property(DbContext context, Type entityType, string propertyName) =>
        context.Model.FindEntityType(entityType)!.FindProperty(propertyName)!;

    private sealed class ConsumerDbContext : DbContextBase
    {
        public ConsumerDbContext(DbContextOptions<ConsumerDbContext> options, IOptions<OutboxOptions> outboxOptions)
            : base(options, outboxOptions) { }

        public DbSet<ConsumerEntity> Entities => Set<ConsumerEntity>();
    }

    private sealed class MovedOutboxDbContext : DbContextBase
    {
        public MovedOutboxDbContext(DbContextOptions<MovedOutboxDbContext> options, IOptions<OutboxOptions> outboxOptions)
            : base(options, outboxOptions) { }

        public DbSet<ConsumerEntity> Entities => Set<ConsumerEntity>();
    }

    private sealed class ConsumerEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
