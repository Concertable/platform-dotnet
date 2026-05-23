namespace Concertable.Messaging.UnitTests;

public class InboxMessageEntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MessageId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private const string ConsumerName = "SomeProjectionHandler";
    private const string MessageType = "Concertable.B2B.Venue.Contracts.Events.VenueChangedEvent";

    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange + Act
        var entity = InboxMessageEntity.Create(MessageId, ConsumerName, MessageType, Now);

        // Assert
        Assert.Equal(MessageId, entity.MessageId);
        Assert.Equal(ConsumerName, entity.ConsumerName);
        Assert.Equal(MessageType, entity.MessageType);
        Assert.Equal(Now, entity.ReceivedAt);
    }

    [Fact]
    public void Create_WithDifferentConsumerSameMsgId_ProducesIndependentRow()
    {
        // Arrange
        var row1 = InboxMessageEntity.Create(MessageId, "ConsumerA", MessageType, Now);
        var row2 = InboxMessageEntity.Create(MessageId, "ConsumerB", MessageType, Now);

        // Assert — same MessageId, different ConsumerName → independent rows (composite key)
        Assert.Equal(row1.MessageId, row2.MessageId);
        Assert.NotEqual(row1.ConsumerName, row2.ConsumerName);
    }
}
