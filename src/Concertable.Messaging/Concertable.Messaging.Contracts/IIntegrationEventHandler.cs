namespace Concertable.Messaging.Contracts;

public interface IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, MessageEnvelope envelope, CancellationToken ct = default);
}
