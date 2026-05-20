namespace Concertable.Messaging;

public sealed record MessageEnvelope(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId = null)
{
    public static string TypeNameFor(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.FullName
            ?? throw new ArgumentException(
                $"Message type must be concrete, non-generic (got {type.Name}).", nameof(type));
    }
}
