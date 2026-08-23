using System.Xml.Linq;

namespace Concertable.Testing.Architecture;

public static class ExecutableHostInventory
{
    public static void Validate(string scopePath, params string[] coveredProjects)
    {
        var scope = Path.GetFullPath(scopePath);
        var covered = coveredProjects
            .Select(path => Path.GetFullPath(Path.Combine(scope, path)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var discovered = Directory
            .EnumerateFiles(scope, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(IsExecutable)
            .ToArray();
        var violations = new List<string>();

        foreach (var project in discovered)
        {
            var exclusion = ReadProperty(project, "CompositionValidationExclusion");
            if (covered.Contains(project))
            {
                if (!string.IsNullOrWhiteSpace(exclusion))
                    violations.Add($"{project} is both covered and excluded.");
                if (!HasStrictProviderValidation(project))
                    violations.Add($"{project} does not enable strict service-provider validation.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(exclusion))
                violations.Add($"{project} has neither composition-test coverage nor a CompositionValidationExclusion.");
        }

        foreach (var project in covered.Where(path => !discovered.Contains(path, StringComparer.OrdinalIgnoreCase)))
            violations.Add($"{project} is declared as covered but is not an executable project.");

        if (violations.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, violations));
    }

    public static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "api")) &&
                    (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))))
                    return directory.FullName;

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }

    private static bool IsExecutable(string projectPath)
    {
        var project = XDocument.Load(projectPath);
        var sdk = project.Root?.Attribute("Sdk")?.Value;
        var sdkElements = project.Descendants("Sdk").Select(element => element.Attribute("Name")?.Value);

        return string.Equals(ReadProperty(project, "OutputType"), "Exe", StringComparison.OrdinalIgnoreCase) ||
               !string.IsNullOrWhiteSpace(ReadProperty(project, "AzureFunctionsVersion")) ||
               string.Equals(ReadProperty(project, "IsAspireHost"), "true", StringComparison.OrdinalIgnoreCase) ||
               sdk?.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) == true ||
               sdk?.Contains("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase) == true ||
               sdkElements.Any(value => value?.Contains("Aspire.AppHost.Sdk", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string? ReadProperty(string projectPath, string propertyName) =>
        ReadProperty(XDocument.Load(projectPath), propertyName);

    private static bool HasStrictProviderValidation(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        return Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .Any(source => source.Contains("AddServiceDefaults(", StringComparison.Ordinal) ||
                           source.Contains("UseStrictServiceProviderValidation(", StringComparison.Ordinal) ||
                           source.Contains("ServiceProviderValidation.CreateFactory(", StringComparison.Ordinal) ||
                           source.Contains("StrictDistributedApplication.CreateBuilder(", StringComparison.Ordinal));
    }

    private static string? ReadProperty(XDocument project, string propertyName) =>
        project.Descendants(propertyName).Select(element => element.Value.Trim()).LastOrDefault();
}
