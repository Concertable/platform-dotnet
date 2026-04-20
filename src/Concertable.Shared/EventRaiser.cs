namespace Concertable.Shared;

public class EventRaiser
{
    private readonly List<IDomainEvent> events = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => events.AsReadOnly();

    public void Raise(IDomainEvent e) => events.Add(e);

    public void Clear() => events.Clear();
}
