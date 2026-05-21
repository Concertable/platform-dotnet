using Concertable.Messaging.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.Infrastructure.Outbox;

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

        var envelope = new MessageEnvelope(
            Guid.NewGuid(),
            typeof(TEvent).FullName!,
            timeProvider.GetUtcNow(),
            null);

        foreach (var handler in serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.HandleAsync(@event, envelope, ct);
    }

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : IIntegrationCommand =>
        outbox.SendAsync(command, ct);
}
