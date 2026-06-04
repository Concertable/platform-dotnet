using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Application;

/// <summary>
/// Transactional-outbox <see cref="IBus"/> implementation: serializes the message and stages it as an
/// <see cref="OutboxMessageEntity"/> row on the caller's ambient <c>DbContext</c>, so dispatch commits
/// atomically with the producing business transaction. <c>OutboxDispatcher</c> subsequently forwards
/// committed rows to <c>IBusTransport</c> with retry and dead-lettering — same destination as <c>Bus</c>,
/// one durable hop earlier.
/// </summary>
internal sealed class OutboxBus : IBus
{
    private readonly IOutboxWriter writer;
    private readonly MessageSerializer serializer;
    private readonly TimeProvider timeProvider;

    public OutboxBus(IOutboxWriter writer, MessageSerializer serializer, TimeProvider timeProvider)
    {
        this.writer = writer;
        this.serializer = serializer;
        this.timeProvider = timeProvider;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent =>
        EnqueueAsync(@event, typeof(TEvent), MessageKind.Event, ct);

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : IIntegrationCommand =>
        EnqueueAsync(command, typeof(TCommand), MessageKind.Command, ct);

    private Task EnqueueAsync<T>(T message, Type messageType, MessageKind kind, CancellationToken ct)
    {
        var payload = serializer.Serialize(message).ToString();
        var row = OutboxMessageEntity.Create(messageType, payload, timeProvider.GetUtcNow(), kind);
        return writer.AddAsync(row, ct);
    }
}
