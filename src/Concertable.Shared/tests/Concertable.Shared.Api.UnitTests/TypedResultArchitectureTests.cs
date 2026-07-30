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

    private static bool IsProductionSource(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}src{separator}", StringComparison.OrdinalIgnoreCase)
            && !path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
            && !path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase);
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

    [GeneratedRegex(@"\b(?:Result<[^,\r\n>]+,\s*[^>\r\n]+>|UnitResult<[^>\r\n]+>)")]
    private static partial Regex TypedResultPattern();

    [GeneratedRegex(
        @"\b(?:HttpException|BadRequestException|NotFoundException|ConflictException|ForbiddenException|PaymentRequiredException|InternalServerException)\b|\.OrNotFound\s*\(")]
    private static partial Regex HttpExceptionPattern();

    [GeneratedRegex(@"\[\s*Union(?:Attribute)?(?:\s*\(|\s*\])")]
    private static partial Regex UnionAttributePattern();
}
