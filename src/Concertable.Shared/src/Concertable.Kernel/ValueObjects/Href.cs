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

        var trimmed = value.Trim();

        if (!trimmed.StartsWith('/'))
            return Validation.Invalid($"'{value}' must be root-relative.");

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
            return Validation.Invalid($"'{value}' must not be protocol-relative.");

        if (trimmed.Split('/').Contains(".."))
            return Validation.Invalid($"'{value}' must not traverse its parent.");

        return Uri.TryCreate(trimmed, UriKind.Relative, out _)
            ? Validation.Ok
            : Validation.Invalid($"'{value}' is not a valid relative URL.");
    }
}
