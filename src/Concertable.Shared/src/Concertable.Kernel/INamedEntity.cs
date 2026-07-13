namespace Concertable.Kernel;

/// <summary>Opt-in marker: an entity that carries its own human-readable name for "not found" messages,
/// so <c>OrNotFound()</c> needs no label at the call site and no type-name reflection. Static because the
/// name is read off the type <c>T</c> when the value is absent (a null fetch throwing a 404) — there is
/// no instance.
///
/// Deliberately a SEPARATE interface, NOT a member on <see cref="IEntity"/>. A required
/// (<c>static abstract</c>) member on the base <c>IEntity</c> is a binary-breaking change that cannot
/// land here: the core libs (DataAccess, Messaging) reference Kernel by source, so every integration test
/// loads the new Kernel, while service module entities compile against the published Kernel *package* —
/// their static-abstract implementation mapping is fixed at compile time against the old interface, so
/// they throw <c>TypeLoadException</c> at runtime regardless of any <c>DisplayName</c> in their source.
/// An opt-in marker adds nothing to <c>IEntity</c>, so no existing entity's type-load ever changes.</summary>
public interface INamedEntity : IEntity
{
    static abstract string DisplayName { get; }
}
