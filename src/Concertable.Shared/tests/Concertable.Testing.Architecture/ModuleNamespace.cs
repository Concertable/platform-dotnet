namespace Concertable.Testing.Architecture;

/// <summary>
/// One module-owned namespace within a service — <c>("Dashboard.Artist", Api)</c> for
/// <c>Concertable.B2B.Dashboard.Artist.Api</c>. The company and service are identical for every namespace in
/// a <see cref="ServiceArchitecture"/>, so they live there rather than being repeated here.
/// </summary>
public readonly record struct ModuleNamespace(string Module, ArchitectureLayer Layer);
