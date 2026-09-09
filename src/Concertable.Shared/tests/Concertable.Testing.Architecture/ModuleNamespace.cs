namespace Concertable.Testing.Architecture;

/// <summary>
/// One module-owned namespace within a service — <c>("Dashboard.Artist", Api)</c> for
/// <c>Concertable.B2B.Dashboard.Artist.Api</c>. The company and service are identical for every namespace in
/// a <see cref="ServiceArchitecture"/>, so they live there rather than being repeated here.
/// </summary>
public readonly record struct ModuleNamespace(string Module, ArchitectureLayer Layer)
{
    /// <summary>
    /// Parses a fully-qualified assembly or project name — <c>Concertable.B2B.Concert.Domain</c> — given its
    /// owning <paramref name="company"/> and <paramref name="service"/>, or <c>null</c> when it is not one of
    /// that service's module namespaces (wrong company/service, no layer suffix, or no module segment).
    /// </summary>
    public static ModuleNamespace? Parse(string qualifiedName, string company, string service)
    {
        var prefix = $"{company}.{service}.";
        if (!qualifiedName.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var rest = qualifiedName[prefix.Length..];
        var lastDot = rest.LastIndexOf('.');
        if (lastDot < 0)
            return null;

        return Enum.TryParse<ArchitectureLayer>(rest[(lastDot + 1)..], out var layer) && Enum.IsDefined(layer)
            ? new ModuleNamespace(rest[..lastDot], layer)
            : null;
    }
}
