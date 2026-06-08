namespace Concertable.Messaging.Contracts;

/// <summary>
/// Transfers a single serialized message to its destination: events to topics, commands to queues.
/// Called only by messaging infrastructure (<c>Bus</c>, <c>OutboxDispatcher</c>); application code uses
/// <see cref="IBus"/>. Implementations are environment-specific: Azure Service Bus in hosts, in-memory
/// handler dispatch in integration tests.
/// </summary>
public interface IBusTransport
{
    Task PublishAsync<TEvent>(TEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;

    Task SendAsync<TCommand>(TCommand command, MessageEnvelope envelope, CancellationToken ct = default)
        where TCommand : IIntegrationCommand;
}
