using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Domain;

public sealed class OutboxMessageEntity : IGuidEntity
{
    private OutboxMessageEntity() { }

    public Guid Id { get; private set; }
    public string MessageType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }
    public MessageKind Kind { get; private set; }
    public OutboxStatus Status { get; private set; }
    public DateTimeOffset? DispatchedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset? NextRetryAtUtc { get; private set; }

    public static OutboxMessageEntity Create(
        Type messageType,
        string payload,
        DateTimeOffset occurredAtUtc,
        MessageKind kind,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new DomainException("Payload is required.");
        return new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            MessageType = MessageTypeAttribute.Resolve(messageType),
            Payload = payload,
            OccurredAtUtc = occurredAtUtc,
            Kind = kind,
            CorrelationId = correlationId,
            Status = OutboxStatus.Pending,
        };
    }

    public void MarkDispatched(DateTimeOffset when)
    {
        if (Status is OutboxStatus.Dispatched) return;
        if (Status is OutboxStatus.DeadLettered)
            throw new DomainException("Cannot dispatch a dead-lettered message.");
        Status = OutboxStatus.Dispatched;
        DispatchedAtUtc = when;
        LastError = null;
    }

    public void RecordFailure(string error, int maxAttempts, DateTimeOffset now)
    {
        if (Status is OutboxStatus.Dispatched)
            throw new DomainException("Cannot record failure on a dispatched message.");
        if (string.IsNullOrWhiteSpace(error))
            throw new DomainException("Error is required.");
        Attempts++;
        LastError = error;
        if (Attempts >= maxAttempts)
            Status = OutboxStatus.DeadLettered;
        else
            NextRetryAtUtc = now.AddSeconds(Math.Min(Math.Pow(2, Attempts - 1), 300));
    }
}
