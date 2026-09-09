using System.Text.RegularExpressions;

namespace Concertable.Shared.Api.UnitTests;

public sealed partial class RepositoryArchitectureTests
{
    [Fact]
    public void ProductionSource_DeclaresOneReadContextContract()
    {
        var declarations = EnumerateProductionSource()
            .Where(path => ReadContextDeclarationPattern().IsMatch(File.ReadAllText(path)))
            .ToArray();

        var declaration = Assert.Single(declarations);
        Assert.EndsWith(
            "Concertable.DataAccess/Concertable.DataAccess.Application/IReadDbContext.cs",
            declaration.Replace(Path.DirectorySeparatorChar, '/'),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSource_DeclaresOneGenericReadRepositoryImplementation()
    {
        var declarations = EnumerateProductionSource()
            .Where(path => ReadRepositoryDeclarationPattern().IsMatch(File.ReadAllText(path)))
            .ToArray();

        var declaration = Assert.Single(declarations);
        Assert.EndsWith(
            "Concertable.DataAccess/Concertable.DataAccess.Infrastructure/Repository.cs",
            declaration.Replace(Path.DirectorySeparatorChar, '/'),
            StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateProductionSource()
    {
        var sourceRoot = FindSourceRoot();
        return Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase));
    }

    private static string FindSourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Concertable.Platform.slnx")))
                return Path.Combine(directory.FullName, "src");

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Concertable.Platform.slnx.");
    }

    [GeneratedRegex(@"\binterface\s+IReadDbContext\b")]
    private static partial Regex ReadContextDeclarationPattern();

    [GeneratedRegex(@"\bclass\s+ReadRepository\s*<")]
    private static partial Regex ReadRepositoryDeclarationPattern();
}
