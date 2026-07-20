namespace Concertable.Kernel;

/// <summary>
/// An entity whose primary key <em>is</em> the id of the thing it belongs to — one row per owner
/// (e.g. a per-tenant counter), not a surrogate-keyed row that merely references its owner. Lets a
/// repository find one by owner without knowing the concrete type; <typeparamref name="TKey"/> is the
/// owner's key type.
/// </summary>
public interface IOwned<TKey>
{
    TKey OwnerId { get; }
}
