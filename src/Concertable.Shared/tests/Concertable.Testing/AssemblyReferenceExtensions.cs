using System.Reflection;

namespace Concertable.Testing;

public static class AssemblyReferenceExtensions
{
    extension(Assembly assembly)
    {
        public DirectoryInfo OutputDirectory =>
            new FileInfo(assembly.Location).Directory!;

        public DirectoryInfo SolutionDirectory =>
            assembly.OutputDirectory.NearestSolutionDirectory;

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

        public IReadOnlyCollection<string> CrossModuleDomainOrInfrastructureReferences(
            IEnumerable<Assembly> moduleAssemblies)
        {
            var assemblyNameParts = assembly.GetName().Name!.Split('.');
            if (assemblyNameParts.Length < 4)
                throw new InvalidOperationException($"{assembly.FullName} is not a module-owned assembly.");

            var owner = assemblyNameParts[2];
            var modules = moduleAssemblies
                .Select(moduleAssembly => moduleAssembly.GetName().Name!.Split('.')[2])
                .ToHashSet(StringComparer.Ordinal);

            return assembly.ReferencedAssemblyNames()
                .Where(referenceName => referenceName.Split('.') is
                    [var product, var service, var module, "Domain" or "Infrastructure", ..] &&
                    product == assemblyNameParts[0] &&
                    service == assemblyNameParts[1] &&
                    modules.Contains(module) &&
                    module != owner)
                .ToArray();
        }

        public IReadOnlyCollection<Assembly> LoadSiblingModuleIntegrationTestAssemblies()
        {
            var assemblyNameParts = assembly.GetName().Name!.Split('.');
            if (assemblyNameParts.Length < 3)
                throw new InvalidOperationException($"{assembly.FullName} has no service identity.");

            var servicePrefix = $"{assemblyNameParts[0]}.{assemblyNameParts[1]}";

            return assembly.OutputDirectory
                .EnumerateFiles($"{servicePrefix}.*.IntegrationTests.dll")
                .Select(file => file.FullName)
                .Select(Assembly.LoadFrom)
                .Where(candidate => candidate.GetName().Name?.Split('.') is
                    [var product, var service, _, "IntegrationTests"] &&
                    product == assemblyNameParts[0] &&
                    service == assemblyNameParts[1])
                .ToArray();
        }
    }
}
