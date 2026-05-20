using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Application;

internal sealed class OutboxBus : IBus
{
    private readonly IOutboxStore store;
    private readonly MessageSerializer serializer;
    private readonly TimeProvider timeProvider;

    public OutboxBus(IOutboxStore store, MessageSerializer serializer, TimeProvider timeProvider)
    {
        this.store = store;
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
        return store.AddAsync(row, ct);
    }
}
