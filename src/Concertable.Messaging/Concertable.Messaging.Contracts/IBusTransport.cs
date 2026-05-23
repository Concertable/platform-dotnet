namespace Concertable.Messaging.Contracts;

public interface IBusTransport
{
    Task PublishAsync<TEvent>(TEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;

    Task SendAsync<TCommand>(TCommand command, MessageEnvelope envelope, CancellationToken ct = default)
        where TCommand : IIntegrationCommand;
}
