namespace Concertable.Testing.Architecture;

/// <summary>
/// The module layers, innermost first. <see cref="Infrastructure"/> and <see cref="Api"/> are peers — both
/// sit on <see cref="Application"/> and neither depends on the other.
/// </summary>
public enum ArchitectureLayer
{
    Contracts,
    Domain,
    Application,
    Infrastructure,
    Api,
}
