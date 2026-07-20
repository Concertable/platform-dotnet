using System.Net.Mail;
using Vogen;

namespace Concertable.Kernel.ValueObjects;

[ValueObject<string>(throws: typeof(DomainException),
    conversions: Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public sealed partial record EmailAddress
{
    private static string NormalizeInput(string input) => input.Trim().ToLowerInvariant();

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid("Email is required.");

        var trimmed = value.Trim();
        // MailAddress also parses "Name <a@b>" / comment forms — the round-trip keeps us to a bare address.
        return MailAddress.TryCreate(trimmed, out var parsed)
            && string.Equals(parsed.Address, trimmed, StringComparison.OrdinalIgnoreCase)
                ? Validation.Ok
                : Validation.Invalid($"'{value}' is not a valid email address.");
    }
}
