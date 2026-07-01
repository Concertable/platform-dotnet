using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Kernel.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public Task DispatchPreCommitAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default) =>
        DispatchPhaseAsync(events, preCommitOnly: true, ct);

    public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default) =>
        DispatchPhaseAsync(events, preCommitOnly: false, ct);

    private async Task DispatchPhaseAsync(IEnumerable<IDomainEvent> events, bool preCommitOnly, CancellationToken ct)
    {
        foreach (var @event in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());
            var preCommitType = typeof(IPreCommitDomainEventHandler<>).MakeGenericType(@event.GetType());
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            foreach (var handler in serviceProvider.GetServices(handlerType))
            {
                if (preCommitType.IsAssignableFrom(handler!.GetType()) == preCommitOnly)
                    await (Task)method.Invoke(handler, [@event, ct])!;
            }
        }
    }
}
