namespace Concertable.Messaging.Contracts;

public sealed record MessageEnvelope(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId = null)
{
    public static MessageEnvelope Create<TMessage>(DateTimeOffset occurredAtUtc, string? correlationId = null) =>
        new(Guid.NewGuid(),
            MessageTypeAttribute.Resolve(typeof(TMessage)),
            occurredAtUtc,
            correlationId);
}
