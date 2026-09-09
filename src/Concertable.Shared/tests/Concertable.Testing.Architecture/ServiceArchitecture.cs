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
        IReadOnlyList<ModuleNamespace> namespaces,
        IReadOnlyList<Assembly> moduleAssemblies)
    {
        Company = company;
        Service = service;
        Assemblies = assemblies;
        Namespaces = namespaces;
        ModuleAssemblies = moduleAssemblies;
        Modules = namespaces.Select(module => module.Module).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The company segment — <c>Concertable</c>.</summary>
    public string Company { get; }

    /// <summary>The service segment — <c>B2B</c>.</summary>
    public string Service { get; }

    /// <summary>
    /// Every non-test <c>{Company}.*</c> assembly in the test output — the graph ArchUnitNET loads. Wider
    /// than the service's own modules so it also carries the shared platform libraries (kernel, messaging)
    /// the module assemblies depend on, letting ArchUnitNET resolve those edges.
    /// </summary>
    public IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>Every module-owned namespace, audience-facing modules only — the parsed source <see cref="Modules"/> is a view of.</summary>
    public IReadOnlyList<ModuleNamespace> Namespaces { get; }

    /// <summary>The assemblies behind <see cref="Namespaces"/> — for reflection restricted to module types.</summary>
    public IReadOnlyList<Assembly> ModuleAssemblies { get; }

    /// <summary>The distinct module paths — <c>Concert</c>, <c>Dashboard.Artist</c>, … — the <see cref="ModuleNamespace.Module"/> values of <see cref="Namespaces"/>.</summary>
    public IReadOnlySet<string> Modules { get; }

    /// <summary>Builds the topology for the service that owns <paramref name="testAssembly"/> (its first two name segments).</summary>
    public static ServiceArchitecture Create(Assembly testAssembly)
    {
        var parts = testAssembly.GetName().Name!.Split('.');
        var company = parts[0];
        var service = parts[1];

        var directory = Path.GetDirectoryName(testAssembly.Location)!;
        var assemblies = Directory.GetFiles(directory, $"{company}.*.dll")
            .Where(path => !Path.GetFileNameWithoutExtension(path).Contains("Test", StringComparison.Ordinal))
            .Select(Assembly.LoadFrom)
            .ToArray();

        var parsed = assemblies
            .Select(assembly => (Assembly: assembly, Namespace: ModuleNamespace.Parse(assembly.GetName().Name!, company, service)))
            .Where(entry => entry.Namespace is not null)
            .Select(entry => (entry.Assembly, Namespace: entry.Namespace!.Value))
            .ToArray();

        var audienceFacing = parsed
            .GroupBy(entry => entry.Namespace.Module, StringComparer.Ordinal)
            .Where(group => group.Any(entry => DefiningLayers.Contains(entry.Namespace.Layer)))
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (audienceFacing.Count == 0)
            throw new InvalidOperationException(
                $"No {company}.{service} module assemblies (a Domain or Api layer) were found in '{directory}'. " +
                "Architecture rules built from an empty module set enforce nothing — check the test project's references.");

        var modules = parsed.Where(entry => audienceFacing.Contains(entry.Namespace.Module)).ToArray();

        return new ServiceArchitecture(
            company,
            service,
            assemblies,
            modules
                .Select(entry => entry.Namespace)
                .OrderBy(module => module.Module, StringComparer.Ordinal)
                .ThenBy(module => module.Layer)
                .ToArray(),
            modules.Select(entry => entry.Assembly).ToArray());
    }

    /// <summary>
    /// This service's <see cref="ModuleNamespace.Parse(string, string, string)"/> — parses
    /// <c>Concertable.B2B.Concert.Domain</c> into its module namespace, or <c>null</c> when it is not one of
    /// this service's.
    /// </summary>
    public ModuleNamespace? Parse(string qualifiedName) => ModuleNamespace.Parse(qualifiedName, Company, Service);

    /// <summary>A namespace regex matching types in the given layers of any module — its whole subtree when no layer is named.</summary>
    public string NamespacePattern(params ArchitectureLayer[] layers) =>
        Pattern($"({string.Join("|", Modules.Select(Regex.Escape))})", layers);

    /// <summary>A namespace regex matching types in the given layers of one module — its whole subtree when no layer is named.</summary>
    public string NamespacePattern(string module, params ArchitectureLayer[] layers) =>
        Pattern(Regex.Escape(module), layers);

    private string Pattern(string moduleGroup, ArchitectureLayer[] layers)
    {
        var head = $@"^{Regex.Escape(Company)}\.{Regex.Escape(Service)}\.{moduleGroup}";
        return layers.Length == 0
            ? $@"{head}($|\.)"
            : $@"{head}\.({string.Join("|", layers)})($|\.)";
    }
}
