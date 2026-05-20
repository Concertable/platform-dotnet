using Concertable.Messaging.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.Infrastructure;

internal sealed class InMemoryBusTransport : IBusTransport
{
    private readonly IServiceProvider serviceProvider;

    public InMemoryBusTransport(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var handlers = serviceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(@event, envelope, ct);
    }

    public async Task SendAsync<TCommand>(TCommand command, MessageEnvelope envelope, CancellationToken ct = default)
        where TCommand : IIntegrationCommand
    {
        var handler = serviceProvider.GetService<IIntegrationCommandHandler<TCommand>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command {typeof(TCommand).FullName}. Commands require exactly one handler.");
        await handler.HandleAsync(command, envelope, ct);
    }
}
