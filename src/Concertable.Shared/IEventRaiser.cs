namespace Concertable.Shared;

public interface IEventRaiser
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
