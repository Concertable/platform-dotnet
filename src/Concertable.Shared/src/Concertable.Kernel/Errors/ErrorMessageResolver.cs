using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Concertable.Kernel.Errors;

internal static partial class ErrorMessageResolver
{
    private static readonly ConcurrentDictionary<Type, string> cache = new();

    internal static string Of<TCase>() =>
        cache.GetOrAdd(typeof(TCase), Resolve);

    private static string Resolve(Type caseType)
    {
        var caseName = caseType.Name.EndsWith("Case", StringComparison.Ordinal)
            ? caseType.Name[..^"Case".Length]
            : caseType.Name;
        var words = WordPattern()
            .Matches(caseName)
            .Select(match => match.Value)
            .ToArray();

        if (words.Length == 0
            || !string.Concat(words).Equals(caseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{caseType.Name} cannot be humanized into a stable error message.");
        }

        return $"{words[0]} {string.Join(' ', words.Skip(1).Select(word => word.ToLowerInvariant()))}"
            .TrimEnd()
            + ".";
    }

    [GeneratedRegex(
        @"[A-Z]+(?=[A-Z][a-z]|\d|$)|[A-Z]?[a-z]+|\d+",
        RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
