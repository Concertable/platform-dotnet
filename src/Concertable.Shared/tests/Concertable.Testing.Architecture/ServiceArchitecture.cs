using System.Reflection;
using System.Text.RegularExpressions;

namespace Concertable.Testing.Architecture;

/// <summary>
/// A service's module topology, read from the loaded assembly graph rather than a hand-maintained list —
/// nothing here enumerates modules by name, so a rename, a move or a new module needs no test change. A
/// module is a namespace that ships a <see cref="ArchitectureLayer.Domain"/> or an
/// <see cref="ArchitectureLayer.Api"/> layer: that separates an audience-facing module from a shared
/// library such as DataAccess or Seed.
/// </summary>
public sealed class ServiceArchitecture
{
    private static readonly ArchitectureLayer[] DefiningLayers = [ArchitectureLayer.Domain, ArchitectureLayer.Api];

    private ServiceArchitecture(
        string company,
        string service,
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyList<ModuleNamespace> namespaces)
    {
        this.Company = company;
        this.Service = service;
        this.Assemblies = assemblies;
        this.Namespaces = namespaces;
        this.Modules = namespaces.Select(module => module.Module).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The company segment — <c>Concertable</c>.</summary>
    public string Company { get; }

    /// <summary>The service segment — <c>B2B</c>.</summary>
    public string Service { get; }

    /// <summary>
    /// Every non-test <c>{Company}.{Service}.*</c> assembly in the test output, plus the kernel — the graph
    /// ArchUnitNET loads.
    /// </summary>
    public IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>Every module-owned namespace, audience-facing modules only — the parsed source <see cref="Modules"/> is a view of.</summary>
    public IReadOnlyList<ModuleNamespace> Namespaces { get; }

    /// <summary>The distinct module paths — <c>Concert</c>, <c>Dashboard.Artist</c>, … — the <see cref="ModuleNamespace.Module"/> values of <see cref="Namespaces"/>.</summary>
    public IReadOnlySet<string> Modules { get; }

    /// <summary>Builds the topology for the service that owns <paramref name="testAssembly"/> (its first two name segments).</summary>
    public static ServiceArchitecture Create(Assembly testAssembly)
    {
        var parts = testAssembly.GetName().Name!.Split('.');
        var company = parts[0];
        var service = parts[1];
        var prefix = $"{company}.{service}.";

        var directory = Path.GetDirectoryName(testAssembly.Location)!;
        var assemblies = Directory.GetFiles(directory, $"{prefix}*.dll")
            .Where(path => !Path.GetFileNameWithoutExtension(path).Contains("Test", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .Append(Assembly.LoadFrom(Path.Combine(directory, $"{company}.Kernel.dll")))
            .ToArray();

        var parsed = assemblies
            .Select(assembly => Parse(assembly.GetName().Name!, prefix))
            .OfType<ModuleNamespace>()
            .ToArray();

        var audienceFacing = parsed
            .GroupBy(module => module.Module, StringComparer.Ordinal)
            .Where(group => group.Any(module => DefiningLayers.Contains(module.Layer)))
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        return new ServiceArchitecture(
            company,
            service,
            assemblies,
            parsed
                .Where(module => audienceFacing.Contains(module.Module))
                .OrderBy(module => module.Module, StringComparer.Ordinal)
                .ThenBy(module => module.Layer)
                .ToArray());
    }

    /// <summary>A namespace regex matching types in the given layers of any module — its whole subtree when no layer is named.</summary>
    public string NamespacePattern(params ArchitectureLayer[] layers) =>
        Pattern($"({string.Join("|", this.Modules.Select(Regex.Escape))})", layers);

    /// <summary>A namespace regex matching types in the given layers of one module — its whole subtree when no layer is named.</summary>
    public string NamespacePattern(string module, params ArchitectureLayer[] layers) =>
        Pattern(Regex.Escape(module), layers);

    private string Pattern(string moduleGroup, ArchitectureLayer[] layers)
    {
        var head = $@"^{Regex.Escape(this.Company)}\.{Regex.Escape(this.Service)}\.{moduleGroup}";
        return layers.Length == 0
            ? $@"{head}($|\.)"
            : $@"{head}\.({string.Join("|", layers)})($|\.)";
    }

    // "Concertable.B2B.Dashboard.Artist.Api" with prefix "Concertable.B2B." -> ("Dashboard.Artist", Api); null
    // for anything not shaped like a module namespace (the kernel, a single-segment shared library).
    private static ModuleNamespace? Parse(string assemblyName, string prefix)
    {
        if (!assemblyName.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var rest = assemblyName[prefix.Length..];
        var lastDot = rest.LastIndexOf('.');
        if (lastDot < 0)
            return null;

        return Enum.TryParse<ArchitectureLayer>(rest[(lastDot + 1)..], out var layer)
            ? new ModuleNamespace(rest[..lastDot], layer)
            : null;
    }
}
