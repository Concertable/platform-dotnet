using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.Infrastructure.Outbox;

/// <summary>
/// Decorates <see cref="OutboxBus"/> with synchronous in-process fan-out: after staging the outbox row,
/// published events are immediately dispatched to every <see cref="IIntegrationEventHandler{TEvent}"/>
/// registered in the current service provider, without waiting for the broker round trip. Remote delivery
/// is unaffected. Commands are not dispatched locally.
/// </summary>
internal sealed class LocalDispatchingBus : IBus
{
    private readonly OutboxBus outbox;
    private readonly IServiceProvider serviceProvider;
    private readonly TimeProvider timeProvider;

    public LocalDispatchingBus(OutboxBus outbox, IServiceProvider serviceProvider, TimeProvider timeProvider)
    {
        this.outbox = outbox;
        this.serviceProvider = serviceProvider;
        this.timeProvider = timeProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        await outbox.PublishAsync(@event, ct);

        var envelope = MessageEnvelope.Create<TEvent>(timeProvider.GetUtcNow());

        foreach (var handler in serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.HandleAsync(@event, envelope, ct);
    }

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : IIntegrationCommand =>
        outbox.SendAsync(command, ct);
}
