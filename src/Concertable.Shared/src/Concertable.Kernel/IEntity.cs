namespace Concertable.Kernel;

public interface IEntity
{
    /// <summary>Human-readable name for "not found" messages, e.g. <c>"Booking agreement"</c>. A
    /// <c>static virtual</c> default interface member, NOT <c>static abstract</c>: making it required
    /// breaks binary compatibility (entity assemblies compiled against the prior <c>IEntity</c> throw
    /// <c>TypeLoadException</c> when loaded against the new one, because the core libs source-reference
    /// Kernel while service entities compile against its published package). A default member is additive,
    /// so nothing breaks; entities that self-name via <c>OrNotFound()</c> override it. The default throws
    /// so a self-name on an un-named entity fails loudly rather than emitting a wrong name.</summary>
    static virtual string DisplayName =>
        throw new NotSupportedException("Entity has no DisplayName; override it or use OrNotFound(label).");
}
