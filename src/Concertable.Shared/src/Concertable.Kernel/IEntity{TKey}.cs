namespace Concertable.Kernel;

public interface IEntity<TKey> : IEntity
{
    TKey Id { get; }
}
