using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Infrastructure;

/// <summary>
/// Direct <see cref="IBus"/> implementation: stamps a <see cref="MessageEnvelope"/> and forwards
/// immediately to <see cref="IBusTransport"/>. Provides no persistence or retry, so dispatch is lost if
/// the process terminates after the call returns; suitable only where no database is available to make
/// dispatch durable. Production hosts register <c>OutboxBus</c> instead.
/// </summary>
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
        transport.PublishAsync(@event, MessageEnvelope.Create<TEvent>(timeProvider.GetUtcNow()), ct);

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : IIntegrationCommand =>
        transport.SendAsync(command, MessageEnvelope.Create<TCommand>(timeProvider.GetUtcNow()), ct);
}
