using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Concertable.Messaging.UnitTests;

public sealed class MessageStoreMappingExtensionsTests
{
    #region MapInboxMessage

    [Theory]
    [InlineData(nameof(InboxMessageEntity.ConsumerName), 256)]
    [InlineData(nameof(InboxMessageEntity.MessageType), 450)]
    public void MapInboxMessage_BoundedString_DeclaresALengthAndNoProviderType(string propertyName, int maxLength)
    {
        var property = InboxProperty(propertyName);

        Assert.Equal(maxLength, property.GetMaxLength());
        Assert.Null(property.GetColumnType());
    }

    #endregion

    #region MapOutboxMessage

    [Theory]
    [InlineData(nameof(OutboxMessageEntity.MessageType), 450)]
    [InlineData(nameof(OutboxMessageEntity.CorrelationId), 450)]
    public void MapOutboxMessage_BoundedString_DeclaresALengthAndNoProviderType(string propertyName, int maxLength)
    {
        var property = OutboxProperty(propertyName);

        Assert.Equal(maxLength, property.GetMaxLength());
        Assert.Null(property.GetColumnType());
    }

    [Theory]
    [InlineData(nameof(OutboxMessageEntity.Payload))]
    [InlineData(nameof(OutboxMessageEntity.LastError))]
    public void MapOutboxMessage_UnboundedString_DeclaresNeitherALengthNorAProviderType(string propertyName)
    {
        var property = OutboxProperty(propertyName);

        Assert.Null(property.GetMaxLength());
        Assert.Null(property.GetColumnType());
    }

    [Fact]
    public void MapOutboxMessage_DispatchQuery_IsIndexedOnStatusAndOccurredAt()
    {
        var index = Assert.Single(OutboxEntity().GetIndexes());

        Assert.Equal(
            [nameof(OutboxMessageEntity.Status), nameof(OutboxMessageEntity.OccurredAtUtc)],
            index.Properties.Select(p => p.Name));
    }

    #endregion

    private static IMutableProperty InboxProperty(string propertyName)
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        modelBuilder.Entity<InboxMessageEntity>().MapInboxMessage();
        return modelBuilder.Model.FindEntityType(typeof(InboxMessageEntity))!.FindProperty(propertyName)!;
    }

    private static IMutableEntityType OutboxEntity()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        modelBuilder.Entity<OutboxMessageEntity>().MapOutboxMessage();
        return modelBuilder.Model.FindEntityType(typeof(OutboxMessageEntity))!;
    }

    private static IMutableProperty OutboxProperty(string propertyName) =>
        OutboxEntity().FindProperty(propertyName)!;
}
