using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Infrastructure;

internal sealed class Bus : IBus
{
    private readonly IBusTransport transport;
    private readonly TimeProvider timeProvider;

    public Bus(IBusTransport transport, TimeProvider timeProvider)
    {
        this.transport = transport;
        this.timeProvider = timeProvider;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent =>
        transport.PublishAsync(@event, BuildEnvelope(typeof(TEvent)), ct);

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : IIntegrationCommand =>
        transport.SendAsync(command, BuildEnvelope(typeof(TCommand)), ct);

    private MessageEnvelope BuildEnvelope(Type messageType) =>
        new(MessageId: Guid.NewGuid(),
            MessageType: MessageEnvelope.TypeNameFor(messageType),
            OccurredAtUtc: timeProvider.GetUtcNow());
}
