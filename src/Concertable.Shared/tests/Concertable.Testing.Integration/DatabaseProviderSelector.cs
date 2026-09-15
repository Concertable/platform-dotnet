using System.Collections.Frozen;

namespace Concertable.Testing.Integration;

/// <summary>
/// Decides which database provider a fixture runs against: the fixture's own declared default, unless
/// configuration names one, so a service declares its provider in code and a run can still be pointed at
/// the other one to compare the two.
/// </summary>
public static class DatabaseProviderSelector
{
    public const string ProviderVariable = "CONCERTABLE_TEST_DB_PROVIDER";

    private static readonly FrozenDictionary<string, DatabaseProvider> ByName =
        Enum.GetValues<DatabaseProvider>()
            .ToFrozenDictionary(provider => provider.ToString(), StringComparer.OrdinalIgnoreCase);

    private static readonly string AllowedNames = string.Join(", ", Enum.GetNames<DatabaseProvider>());

    public static DatabaseProvider Resolve(DatabaseProvider declared, string? configured)
    {
        var validated = Validate(declared);

        return string.IsNullOrWhiteSpace(configured) ? validated : Parse(configured);
    }

    public static DatabaseProvider Parse(string value) =>
        ByName.TryGetValue(value.Trim(), out var parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"{ProviderVariable} is '{value}'; expected one of these names: {AllowedNames}.");

    private static DatabaseProvider Validate(DatabaseProvider declared) =>
        Enum.IsDefined(declared)
            ? declared
            : throw new InvalidOperationException(
                $"The fixture declares the database provider '{declared}'; expected one of these names: {AllowedNames}.");
}
