namespace Concertable.Kernel;

public interface IPreCommitDomainEventHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent { }
