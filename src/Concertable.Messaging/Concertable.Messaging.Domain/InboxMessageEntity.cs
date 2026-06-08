namespace Concertable.Messaging.Domain;

public sealed class InboxMessageEntity
{
    private InboxMessageEntity() { }

    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = null!;
    public string MessageType { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }

    public static InboxMessageEntity Create(Guid messageId, string consumerName, string messageType, DateTimeOffset receivedAt) =>
        new() { MessageId = messageId, ConsumerName = consumerName, MessageType = messageType, ReceivedAt = receivedAt };
}
