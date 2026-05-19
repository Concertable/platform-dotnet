namespace Concertable.Messaging;

public sealed record MessageEnvelope(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId = null);
