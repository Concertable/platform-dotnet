namespace Concertable.Testing.Integration;

/// <summary>
/// Decides which database provider a fixture runs against: the fixture's own declared default, unless
/// configuration names one, so a service declares its provider in code and a run can still be pointed at
/// the other one to compare the two.
/// </summary>
public static class DatabaseProviderSelector
{
    public const string ProviderVariable = "CONCERTABLE_TEST_DB_PROVIDER";

    public static DatabaseProvider Resolve(DatabaseProvider declared, string? configured) =>
        string.IsNullOrWhiteSpace(configured) ? declared : Parse(configured);

    public static DatabaseProvider Parse(string value) =>
        Enum.TryParse<DatabaseProvider>(value.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException(
                $"{ProviderVariable} is '{value}'; expected one of {string.Join(", ", Enum.GetNames<DatabaseProvider>())}.");
}
