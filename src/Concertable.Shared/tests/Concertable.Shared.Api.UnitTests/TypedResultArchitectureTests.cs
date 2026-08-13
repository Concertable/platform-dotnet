using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml.Linq;
using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Results;

namespace Concertable.Shared.Api.UnitTests;

public sealed partial class TypedResultArchitectureTests
{
    [Fact]
    public void TypedResultSlices_DoNotUseHttpExceptions()
    {
        var violations = Directory
            .EnumerateFiles(FindApiRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(IsProductionSource)
            .Where(path => !IsTransitionalTypedResultSlice(path))
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => IsTypedResultHttpExceptionViolation(file.Source))
            .Select(file => file.Path)
            .ToArray();

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(TransitionalTypedResultSlices))]
    public void TransitionalTypedResultSlice_StillMixesHttpException_UntilMigrated(string relativePath)
    {
        var source = File.ReadAllText(Directory
            .EnumerateFiles(FindApiRoot(), "*.cs", SearchOption.AllDirectories)
            .Single(path => path.Replace('\\', '/').EndsWith(relativePath, StringComparison.Ordinal)));

        Assert.True(
            IsTypedResultHttpExceptionViolation(source),
            $"{relativePath} no longer mixes HTTP exceptions with typed results — remove it from the transitional allowlist.");
    }

    [Theory]
    [InlineData("UnitResult<TestError>")]
    [InlineData("Result<TestValue, TestError>")]
    public void TypedResultSlices_ResultWithHttpException_IsDetected(string resultType)
    {
        var source = $$"""
            using Concertable.Kernel.Functional;

            {{resultType}} Execute() => throw new NotFoundException();
            """;

        Assert.True(IsTypedResultHttpExceptionViolation(source));
    }

    [Fact]
    public void FluentResultSlices_OneArityResultWithHttpException_IsIgnored()
    {
        const string source = """
            using FluentResults;

            Result<TestValue> Execute() => throw new NotFoundException();
            """;

        Assert.False(IsTypedResultHttpExceptionViolation(source));
    }

    [Fact]
    public void OwnedValueResultSlices_OneArityResultWithHttpException_IsIgnored()
    {
        const string source = """
            using Concertable.Kernel.Functional;

            Result<TestValue> Execute() => throw new NotFoundException();
            """;

        Assert.False(IsTypedResultHttpExceptionViolation(source));
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
    public void ProblemTerminal_IsGenericOverConcreteErrorType()
    {
        var errorExtensions = typeof(ResultHttpExtensions).Assembly.GetType(
            "Concertable.Shared.Api.Results.ErrorHttpExtensions",
            throwOnError: true)!;
        var method = Assert.Single(
            errorExtensions.GetMethods(BindingFlags.Static | BindingFlags.NonPublic),
            method => method.Name == "ToProblemActionResult");
        var errorType = Assert.Single(method.GetGenericArguments());
        var receiver = Assert.Single(method.GetParameters());

        Assert.True(method.IsGenericMethodDefinition);
        Assert.True(receiver.ParameterType.IsGenericParameter);
        Assert.Equal(errorType, receiver.ParameterType);
        Assert.Contains(typeof(IError), errorType.GetGenericParameterConstraints());
    }

    [Fact]
    public void ServiceHosts_RegisterProblemDetailsBeforeMvc()
    {
        var violations = Directory
            .EnumerateFiles(FindApiRoot(), "Program.cs", SearchOption.AllDirectories)
            .Where(path => Path.GetDirectoryName(path)?
                .EndsWith(".Web", StringComparison.Ordinal) == true)
            .Select(path => new
            {
                Path = path,
                Source = File.ReadAllText(path)
            })
            .Select(host => new
            {
                host.Path,
                ProblemDetails = ProblemDetailsRegistrationPattern().Match(host.Source),
                Mvc = MvcRegistrationPattern().Match(host.Source)
            })
            .Where(host =>
                !host.ProblemDetails.Success
                || !host.Mvc.Success
                || host.ProblemDetails.Index > host.Mvc.Index)
            .Select(host => host.Path)
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
    public void DunetUnionDefinitions_UseSupportedDefinitionShape()
    {
        var violations = EnumerateSourceFiles()
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => UnionAttributePattern().IsMatch(file.Source))
            .Where(file => ErrorUnionPattern().IsMatch(file.Source))
            .Where(file => !UsesSupportedDefinitionShape(file.Source))
            .Select(file => file.Path)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void DunetUnionDefinition_ExistingSupportedShapes_AreAccepted()
    {
        string[] sources =
        [
            "public ErrorDefinition Definition => Match<ErrorDefinition>();",
            "public abstract ErrorDefinition Definition { get; }",
            """
            public ErrorDefinition Definition => this switch
            {
                Missing => ErrorDefinition.NotFound<Missing>()
            };
            """
        ];

        Assert.All(sources, source => Assert.True(UsesSupportedDefinitionShape(source)));
    }

    [Theory]
    [InlineData("_")]
    [InlineData("default")]
    [InlineData("var _")]
    [InlineData("var ignored")]
    public void DunetUnionDefinition_CatchAllSwitchArm_IsRejected(string pattern)
    {
        var source = $$"""
            public ErrorDefinition Definition => this switch
            {
                Missing => ErrorDefinition.NotFound<Missing>(),
                {{pattern}} => ErrorDefinition.Invalid<Fallback>()
            };
            """;

        Assert.False(UsesSupportedDefinitionShape(source));
    }

    [Theory]
    [InlineData("_")]
    [InlineData("var _")]
    [InlineData("var ignored")]
    public void DunetUnionDefinition_UnrelatedCatchAllSwitchArm_IsAccepted(string pattern)
    {
        var source = $$"""
            public ErrorDefinition Definition => this switch
            {
                Missing => ErrorDefinition.NotFound<Missing>()
            };

            public string Code => value switch
            {
                {{pattern}} => "fallback"
            };
            """;

        Assert.True(UsesSupportedDefinitionShape(source));
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

    public static TheoryData<string> TransitionalTypedResultSlices { get; } = new()
    {
        "Concertable.Payment.Infrastructure/CustomerPaymentService.cs",
        "Concertable.Payment.Infrastructure/ManagerPaymentService.cs"
    };

    private static bool IsTransitionalTypedResultSlice(string path)
    {
        var normalized = path.Replace('\\', '/');
        return TransitionalTypedResultSlices
            .Cast<object[]>()
            .Any(row => normalized.EndsWith((string)row[0], StringComparison.Ordinal));
    }

    private static bool IsTypedResultHttpExceptionViolation(string source) =>
        HttpExceptionPattern().IsMatch(source)
        && TypedErrorResultPattern().IsMatch(source);

    private static bool UsesSupportedDefinitionShape(string source) =>
        !DefinitionSwitchCatchAllArmPattern().IsMatch(source)
        && (DefinitionMatchPattern().IsMatch(source)
            || AbstractDefinitionPattern().IsMatch(source)
            || SwitchDefinitionPattern().IsMatch(source));

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

    [GeneratedRegex(@"\b(?:UnitResult<[^>\r\n]+>|Result<[^,\r\n>]+,\s*[^>\r\n]+>)")]
    private static partial Regex TypedErrorResultPattern();

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

    [GeneratedRegex(@"\babstract\s+ErrorDefinition\s+Definition\s*\{")]
    private static partial Regex AbstractDefinitionPattern();

    [GeneratedRegex(@"\bErrorDefinition\s+Definition\s*=>\s*this\s+switch\b")]
    private static partial Regex SwitchDefinitionPattern();

    [GeneratedRegex(
        @"\bErrorDefinition\s+Definition\s*=>\s*this\s+switch\s*\{(?:(?!^[ \t]*\};).)*?(?:(?<=\{)|(?<=,))\s*(?:var\s+(?:_|@?[A-Za-z_]\w*)|_|default)\b\s*(?:when\b(?:(?!=>).)*)?=>",
        RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex DefinitionSwitchCatchAllArmPattern();

    [GeneratedRegex(@"\.AddProblemDetails\s*\(")]
    private static partial Regex ProblemDetailsRegistrationPattern();

    [GeneratedRegex(@"\.Add(?:[A-Za-z]+)?Controllers\s*\(")]
    private static partial Regex MvcRegistrationPattern();
}
