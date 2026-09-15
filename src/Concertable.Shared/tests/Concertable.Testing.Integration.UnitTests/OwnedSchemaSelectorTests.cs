using Respawn.Graph;

namespace Concertable.Testing.Integration.UnitTests;

public sealed class OwnedSchemaSelectorTests
{
    private static readonly string[] Catalog =
        ["public", "search", "messaging", "dbo", "sys", "INFORMATION_SCHEMA", "pg_catalog", "pg_toast"];

    #region Select

    [Fact]
    public void Select_SchemasTheCatalogHas_KeepsThemInOrder() =>
        Assert.Equal(
            ["search", "messaging"],
            OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["search", "messaging"], Catalog));

    [Theory]
    [InlineData(DatabaseProvider.SqlServer)]
    [InlineData(DatabaseProvider.Postgres)]
    public void Select_NoSchemaAtAll_Rejects(DatabaseProvider provider) =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(provider, [], Catalog));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Select_ABlankSchema_Rejects(string schema) =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["search", schema], Catalog));

    [Theory]
    [InlineData("pg_catalog")]
    [InlineData("pg_toast")]
    [InlineData("information_schema")]
    public void Select_APostgresSystemSchema_Rejects(string schema) =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.Postgres, [schema], Catalog));

    [Theory]
    [InlineData("sys")]
    [InlineData("INFORMATION_SCHEMA")]
    [InlineData("guest")]
    [InlineData("db_owner")]
    public void Select_ASqlServerSystemSchema_Rejects(string schema) =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.SqlServer, [schema], Catalog));

    [Fact]
    public void Select_ASystemSchemaTheCatalogHas_RejectsItAsSystemRatherThanMissing()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["pg_catalog"], Catalog));

        Assert.Contains("system schema", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Select_ASchemaTheServiceOwnsThatIsNamedLikeADefault_Keeps() =>
        Assert.Equal(
            ["public"],
            OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["public"], Catalog));

    [Fact]
    public void Select_ASchemaTheCatalogLacks_Rejects() =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["serach"], Catalog));

    [Fact]
    public void Select_ASchemaSpeltInAnotherCase_RejectsBecauseTheCatalogIsTheSpelling() =>
        Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["Search"], Catalog));

    [Fact]
    public void Select_ASchemaTheCatalogLacks_NamesItAndWhatTheCatalogHas()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => OwnedSchemaSelector.Select(DatabaseProvider.Postgres, ["serach"], Catalog));

        Assert.Contains("'serach'", error.Message, StringComparison.Ordinal);
        Assert.Contains("search", error.Message, StringComparison.Ordinal);
    }

    #endregion

    #region TablesToIgnore

    [Theory]
    [InlineData(DatabaseProvider.SqlServer)]
    [InlineData(DatabaseProvider.Postgres)]
    public void TablesToIgnore_EveryOwnedSchema_KeepsItsOwnMigrationHistory(DatabaseProvider provider)
    {
        var ignored = Identities(OwnedSchemaSelector.TablesToIgnore(provider, ["search", "messaging"]));

        Assert.Contains("search.__EFMigrationsHistory", ignored);
        Assert.Contains("messaging.__EFMigrationsHistory", ignored);
    }

    [Theory]
    [InlineData(DatabaseProvider.SqlServer)]
    [InlineData(DatabaseProvider.Postgres)]
    public void TablesToIgnore_TheSharedMessageStores_KeepTheirSeparateHistories(DatabaseProvider provider)
    {
        var ignored = Identities(OwnedSchemaSelector.TablesToIgnore(provider, ["search"]));

        Assert.Contains("messaging.__EFMigrationsHistory_Inbox", ignored);
        Assert.Contains("messaging.__EFMigrationsHistory_Outbox", ignored);
    }

    [Fact]
    public void TablesToIgnore_OnPostgres_KeepsWhatPostgisInstalled() =>
        Assert.Contains(
            "public.spatial_ref_sys",
            Identities(OwnedSchemaSelector.TablesToIgnore(DatabaseProvider.Postgres, ["public"])));

    [Fact]
    public void TablesToIgnore_OnSqlServer_LeavesOutThePostgisTable() =>
        Assert.DoesNotContain(
            "public.spatial_ref_sys",
            Identities(OwnedSchemaSelector.TablesToIgnore(DatabaseProvider.SqlServer, ["public"])));

    [Theory]
    [InlineData(DatabaseProvider.SqlServer)]
    [InlineData(DatabaseProvider.Postgres)]
    public void TablesToIgnore_EveryTable_IsNamedWithItsSchema(DatabaseProvider provider) =>
        Assert.All(
            OwnedSchemaSelector.TablesToIgnore(provider, ["search", "messaging"]),
            table => Assert.False(string.IsNullOrEmpty(table.Schema)));

    #endregion

    private static IReadOnlyList<string> Identities(IEnumerable<Table> tables) =>
        [.. tables.Select(table => $"{table.Schema}.{table.Name}")];
}
