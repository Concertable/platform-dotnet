using System.Reflection;

namespace Concertable.Testing;

public static class AssemblyReferenceExtensions
{
    extension(Assembly assembly)
    {
        public IEnumerable<string> ReferencedAssemblyNames() =>
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .OfType<string>();

        public IReadOnlyCollection<string> ReferencesToAssembliesStartingWith(params string[] prefixes) =>
            assembly.ReferencedAssemblyNames()
                .Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                .ToArray();

        public IReadOnlyCollection<string> ModuleInfrastructureReferences(params string[] allowedModules)
        {
            var assemblyName = assembly.GetName().Name!;
            var servicePrefix = assemblyName[..(assemblyName.LastIndexOf('.') + 1)];
            var allowed = allowedModules
                .Select(module => $"{servicePrefix}{module}.Infrastructure")
                .ToHashSet(StringComparer.Ordinal);

            return assembly.ReferencedAssemblyNames()
                .Where(name => name.StartsWith(servicePrefix, StringComparison.Ordinal)
                    && name.EndsWith(".Infrastructure", StringComparison.Ordinal)
                    && !allowed.Contains(name))
                .ToArray();
        }
    }
}
