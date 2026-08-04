using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Concertable.Kernel.Errors;

/// <summary>
/// Derives and caches the published code of an error case from its own name and its union's name. The
/// union's first word is the code prefix, its remaining words are context, and repeated leading case
/// words are dropped, so <c>EscrowRefundError.EscrowNotFound</c> becomes
/// <c>escrow.refund_not_found</c>. A case whose name has moved on keeps its published code with
/// <see cref="ErrorCodeAttribute"/>.
/// </summary>
internal static partial class ErrorCodeResolver
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    internal static string Of<TCase>() => Of(typeof(TCase));

    internal static string Of(Type caseType) => Cache.GetOrAdd(caseType, Resolve);

    private static string Resolve(Type caseType)
    {
        var unionType = caseType.DeclaringType
            ?? throw new InvalidOperationException(
                $"{caseType.Name} must be declared inside its error union to derive an error code.");

        var unionWords = SplitWords(RemoveUnionSuffix(unionType.Name), unionType);
        var caseWords = SplitWords(RemoveCaseSuffix(caseType.Name), caseType);

        return caseType.GetCustomAttribute<ErrorCodeAttribute>(inherit: false)?.Code
            ?? Derive(caseType, unionWords, caseWords);
    }

    private static string Derive(
        Type caseType,
        IReadOnlyList<string> unionWords,
        IReadOnlyList<string> caseWords)
    {
        var suffixWords = unionWords
            .Skip(1)
            .Concat(WithoutRepeatedContext(caseWords, unionWords))
            .ToArray();

        if (suffixWords.Length == 0)
            throw new InvalidOperationException(
                $"{caseType.Name} only repeats its union's name and leaves no code suffix; rename it "
                + "or declare [ErrorCode].");

        return $"{unionWords[0].ToLowerInvariant()}.{ToSnakeCase(suffixWords)}";
    }

    private static string RemoveUnionSuffix(string unionName)
    {
        if (!unionName.EndsWith("Error", StringComparison.Ordinal)
            || unionName.Length == "Error".Length)
        {
            throw new InvalidOperationException(
                $"{unionName} must be named with a non-empty Error suffix to own error codes.");
        }

        return unionName[..^"Error".Length];
    }

    private static string RemoveCaseSuffix(string caseName) =>
        caseName.EndsWith("Case", StringComparison.Ordinal)
            ? caseName[..^"Case".Length]
            : caseName;

    private static IEnumerable<string> WithoutRepeatedContext(
        IReadOnlyList<string> caseWords,
        IReadOnlyList<string> unionWords)
    {
        var repeated = 0;

        while (repeated < caseWords.Count
               && unionWords.Contains(caseWords[repeated], StringComparer.OrdinalIgnoreCase))
        {
            repeated++;
        }

        return caseWords.Skip(repeated);
    }

    private static IReadOnlyList<string> SplitWords(string name, Type sourceType)
    {
        var words = WordPattern()
            .Matches(name)
            .Select(match => match.Value)
            .ToArray();

        if (words.Length == 0
            || !string.Concat(words).Equals(name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{sourceType.Name} is not spelled in words that convert to a stable error code.");
        }

        return words;
    }

    private static string ToSnakeCase(IEnumerable<string> words) =>
        string.Join('_', words.Select(word => word.ToLowerInvariant()));

    [GeneratedRegex(
        @"[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+",
        RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
