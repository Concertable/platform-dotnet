using Respawn.Graph;

namespace Concertable.Testing.Integration.UnitTests;

public sealed class OwnedSchemaSelectorTests
{
    private static readonly string[] Catalog =
        ["public", "search", "messaging", "pg_catalog", "pg_toast", "information_schema"];

    #region Select

    [Fact]
    public void Select_SchemasTheCatalogHas_KeepsThemInOrder() =>
        Assert.Equal(
            ["search", "messaging"],
            OwnedSchemaSelector.Select(["search", "messaging"], Catalog));

    [Fact]
    public void Select_NoSchemaAtAll_Rejects() =>
        Assert.Throws<InvalidOperationException>(() => OwnedSchemaSelector.Select([], Catalog));

    [Fact]
    public void Select_NoSchemaAtAll_SaysThereIsNoWholeDatabaseReset()
    {
        var error = Assert.Throws<InvalidOperationException>(() => OwnedSchemaSelector.Select([], Catalog));

        Assert.Contains("whole-database reset", error.Message, StringComparison.Ordinal);
        Assert.Contains("messaging", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Select_ABlankSchema_Rejects(string schema) =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(["search", schema], Catalog));

    [Theory]
    [InlineData("pg_catalog")]
    [InlineData("pg_toast")]
    [InlineData("information_schema")]
    public void Select_ASystemSchema_Rejects(string schema) =>
        Assert.Throws<InvalidOperationException>(() => OwnedSchemaSelector.Select([schema], Catalog));

    [Fact]
    public void Select_ASystemSchemaTheCatalogHas_RejectsItAsSystemRatherThanMissing()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(["pg_catalog"], Catalog));

        Assert.Contains("system schema", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Select_ASchemaTheServiceOwnsThatIsNamedLikeADefault_Keeps() =>
        Assert.Equal(["public"], OwnedSchemaSelector.Select(["public"], Catalog));

    [Fact]
    public void Select_ASchemaTheCatalogLacks_Rejects() =>
        Assert.Throws<InvalidOperationException>(() => OwnedSchemaSelector.Select(["serach"], Catalog));

    [Fact]
    public void Select_ASchemaSpeltInAnotherCase_RejectsBecauseTheCatalogIsTheSpelling() =>
        Assert.Throws<InvalidOperationException>(() => OwnedSchemaSelector.Select(["Search"], Catalog));

    [Fact]
    public void Select_ASchemaTheCatalogLacks_NamesItAndWhatTheCatalogHas()
    {
        var error = Assert.Throws<InvalidOperationException>(() => OwnedSchemaSelector.Select(["serach"], Catalog));

        Assert.Contains("'serach'", error.Message, StringComparison.Ordinal);
        Assert.Contains("search", error.Message, StringComparison.Ordinal);
    }

    #endregion

    #region TablesToIgnore

    [Fact]
    public void TablesToIgnore_EveryOwnedSchema_KeepsItsOwnMigrationHistory()
    {
        var ignored = Identities(OwnedSchemaSelector.TablesToIgnore(["search", "messaging"]));

        Assert.Contains("search.__EFMigrationsHistory", ignored);
        Assert.Contains("messaging.__EFMigrationsHistory", ignored);
    }

    [Fact]
    public void TablesToIgnore_TheSharedMessageStores_KeepTheirSeparateHistories()
    {
        var ignored = Identities(OwnedSchemaSelector.TablesToIgnore(["search"]));

        Assert.Contains("messaging.__EFMigrationsHistory_Inbox", ignored);
        Assert.Contains("messaging.__EFMigrationsHistory_Outbox", ignored);
    }

    [Fact]
    public void TablesToIgnore_AnyOwnedSchema_KeepsWhatPostgisInstalled() =>
        Assert.Contains(
            "public.spatial_ref_sys",
            Identities(OwnedSchemaSelector.TablesToIgnore(["search"])));

    [Fact]
    public void TablesToIgnore_EveryTable_IsNamedWithItsSchema() =>
        Assert.All(
            OwnedSchemaSelector.TablesToIgnore(["search", "messaging"]),
            table => Assert.False(string.IsNullOrEmpty(table.Schema)));

    #endregion

    private static IReadOnlyList<string> Identities(IEnumerable<Table> tables) =>
        [.. tables.Select(table => $"{table.Schema}.{table.Name}")];
}
