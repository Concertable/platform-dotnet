using Vogen;

namespace Concertable.Kernel.ValueObjects;

[ValueObject<string>(throws: typeof(DomainException),
    conversions: Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public sealed partial record Href
{
    private static string NormalizeInput(string input) => input.Trim();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Href is required.");

        if (value.Any(char.IsControl))
            return Validation.Invalid($"'{value}' must not contain a control character.");

        var path = value.Split('?', '#')[0];

        // Check the once-decoded path as well. A rule that only sees raw bytes accepts "/%2Fhost",
        // which is "//host" after the single decode a server performs.
        var fault = PathFault(path) ?? PathFault(Uri.UnescapeDataString(path));

        return fault is null ? Validation.Ok : Validation.Invalid($"'{value}' {fault}.");
    }

    // An empty interior segment is rejected outright, not just at index 1: the SPA strips the "/api"
    // prefix before re-issuing, so "/api//host" becomes "//host", which every HTTP client treats as
    // protocol-relative and sends off-origin with the caller's bearer token attached.
    private static string? PathFault(string path) =>
        path.Length == 0 || path[0] != '/' ? "must be root-relative"
            : path.Contains("//", StringComparison.Ordinal) ? "must not contain an empty path segment"
            : path.Contains('\\') ? "must not contain a backslash"
            : path.Any(char.IsControl) ? "must not contain a control character"
            : path.Split('/').Contains("..") ? "must not traverse its parent"
            : null;
}
