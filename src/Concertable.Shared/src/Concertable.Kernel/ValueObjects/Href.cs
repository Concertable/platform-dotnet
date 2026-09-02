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

        if (value.Any(character => char.IsControl(character) || character is '\\' or ' '))
            return Validation.Invalid(
                $"'{value}' must not contain a backslash, a space or a control character.");

        // Don't narrow this to StartsWith("//"): a browser reads '\' as '/' and strips tab/CR/LF
        // before parsing, so "/\host" and "/<tab>/host" both resolve cross-origin.
        if (value[0] != '/' || (value.Length > 1 && value[1] == '/'))
            return Validation.Invalid($"'{value}' must be root-relative.");

        var path = value.Split('?', '#')[0];
        if (Uri.UnescapeDataString(path).Split('/').Contains(".."))
            return Validation.Invalid($"'{value}' must not traverse its parent.");

        return Validation.Ok;
    }
}
