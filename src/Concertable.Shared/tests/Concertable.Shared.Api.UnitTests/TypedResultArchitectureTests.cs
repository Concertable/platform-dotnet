using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Concertable.Shared.Api.UnitTests;

public sealed partial class TypedResultArchitectureTests
{
    [Fact]
    public void TypedResultSlices_DoNotUseHttpExceptions()
    {
        var violations = Directory
            .EnumerateFiles(FindApiRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSource)
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => TypedResultPattern().IsMatch(file.Source))
            .Where(file => HttpExceptionPattern().IsMatch(file.Source))
            .Select(file => file.Path)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void SharedProduction_DoesNotDeclareBusinessUnions()
    {
        var sharedSource = Path.Combine(FindApiRoot(), "Concertable.Shared", "src");
        var unions = Directory
            .EnumerateFiles(sharedSource, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => UnionAttributePattern().IsMatch(File.ReadAllText(path)))
            .ToArray();

        Assert.Empty(unions);
    }

    [Fact]
    public void SharedProduction_DoesNotReferenceDunet()
    {
        var sharedSource = Path.Combine(FindApiRoot(), "Concertable.Shared", "src");
        var projects = Directory
            .EnumerateFiles(sharedSource, "*.csproj", SearchOption.AllDirectories)
            .Where(path => XDocument
                .Load(path)
                .Descendants("PackageReference")
                .Any(reference => string.Equals(
                    (string?)reference.Attribute("Include"),
                    "Dunet",
                    StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.Empty(projects);
    }

    [Fact]
    public void SharedProduction_DoesNotReferenceCSharpFunctionalExtensions()
    {
        var sharedSource = Path.Combine(FindApiRoot(), "Concertable.Shared", "src");
        var violations = Directory
            .EnumerateFiles(sharedSource, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj")
            .Where(path => !IsGeneratedPath(path))
            .Where(path => File.ReadAllText(path).Contains(
                "CSharpFunctionalExtensions",
                StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void KernelFunctionalTypes_DoNotReferenceThirdPartyCarriers()
    {
        var functionalSource = Path.Combine(
            FindApiRoot(),
            "Concertable.Shared",
            "src",
            "Concertable.Kernel",
            "Functional");
        var prohibitedNames = new[]
        {
            "CSharpFunctionalExtensions",
            "FluentResults",
            "OneOf",
            "ErrorOr",
            "LanguageExt",
            "Dunet"
        };
        var violations = Directory
            .EnumerateFiles(functionalSource, "*.cs", SearchOption.AllDirectories)
            .Where(path => prohibitedNames.Any(name => File.ReadAllText(path).Contains(
                name,
                StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void DunetImports_AppearOnlyInUnionDeclarationFiles()
    {
        var violations = EnumerateSourceFiles()
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => DunetImportPattern().IsMatch(file.Source))
            .Where(file => !UnionAttributePattern().IsMatch(file.Source))
            .Select(file => file.Path)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void DunetUnionDefinitions_UseGeneratedMatch()
    {
        var violations = EnumerateSourceFiles()
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => UnionAttributePattern().IsMatch(file.Source))
            .Where(file => ErrorUnionPattern().IsMatch(file.Source))
            .Where(file => !DefinitionMatchPattern().IsMatch(file.Source))
            .Select(file => file.Path)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void OperationErrorCases_AreConstructedThroughFactories()
    {
        var violations = EnumerateSourceFiles()
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => DirectErrorCaseConstructionPattern().IsMatch(file.Source))
            .Select(file => file.Path)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void DunetReferences_BelongToProjectsDeclaringUnions()
    {
        var violations = Directory
            .EnumerateFiles(FindApiRoot(), "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path => XDocument
                .Load(path)
                .Descendants("PackageReference")
                .Any(reference => string.Equals(
                    (string?)reference.Attribute("Include"),
                    "Dunet",
                    StringComparison.OrdinalIgnoreCase)))
            .Where(path => !Directory
                .EnumerateFiles(Path.GetDirectoryName(path)!, "*.cs", SearchOption.AllDirectories)
                .Where(sourcePath => !IsGeneratedPath(sourcePath))
                .Any(sourcePath => UnionAttributePattern().IsMatch(File.ReadAllText(sourcePath))))
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool IsProductionSource(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}src{separator}", StringComparison.OrdinalIgnoreCase)
            && !path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
            && !path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateSourceFiles() =>
        Directory
            .EnumerateFiles(FindApiRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path));

    private static bool IsGeneratedPath(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindApiRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var apiRoot = Path.Combine(directory.FullName, "api");

            if (File.Exists(Path.Combine(apiRoot, "Concertable.slnx")))
                return apiRoot;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate api/Concertable.slnx.");
    }

    [GeneratedRegex(@"\bResult<[^,\r\n>]+,\s*[^>\r\n]+>")]
    private static partial Regex TypedResultPattern();

    [GeneratedRegex(
        @"\b(?:HttpException|BadRequestException|NotFoundException|ConflictException|ForbiddenException|PaymentRequiredException|InternalServerException)\b|\.OrNotFound\s*\(")]
    private static partial Regex HttpExceptionPattern();

    [GeneratedRegex(@"\[\s*Union(?:Attribute)?(?:\s*\(|\s*\])")]
    private static partial Regex UnionAttributePattern();

    [GeneratedRegex(@"\busing\s+Dunet\s*;")]
    private static partial Regex DunetImportPattern();

    [GeneratedRegex(@"\bpartial\s+record\s+\w+Error\s*:\s*IError\b")]
    private static partial Regex ErrorUnionPattern();

    [GeneratedRegex(@"\bDefinition\s*=>\s*Match\s*<\s*ErrorDefinition\s*>")]
    private static partial Regex DefinitionMatchPattern();

    [GeneratedRegex(@"\bnew\s+[A-Za-z_][A-Za-z0-9_]*Error\.[A-Za-z_][A-Za-z0-9_]*\s*\(")]
    private static partial Regex DirectErrorCaseConstructionPattern();
}
