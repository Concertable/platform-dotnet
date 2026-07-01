namespace Concertable.Kernel;

public interface IDomainEventDispatcher
{
    Task DispatchPreCommitAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
