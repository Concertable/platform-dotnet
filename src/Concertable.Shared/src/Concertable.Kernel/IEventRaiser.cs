namespace Concertable.Kernel;

public interface IEventRaiser
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
