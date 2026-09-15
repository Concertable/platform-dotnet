namespace Concertable.Testing.Integration.UnitTests;

public sealed class SqlFixtureTests : IDisposable
{
    private readonly string? configured =
        Environment.GetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable);

    public SqlFixtureTests() =>
        Environment.SetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable, null);

    public void Dispose() =>
        Environment.SetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable, configured);

    private sealed class PostgresFixture : SqlFixture
    {
        protected override DatabaseProvider Provider => DatabaseProvider.Postgres;
    }

    #region ActiveProvider

    [Fact]
    public void ActiveProvider_NothingConfigured_IsTheDefaultDeclaration() =>
        Assert.Equal(DatabaseProvider.SqlServer, new SqlFixture().ActiveProvider);

    [Fact]
    public void ActiveProvider_NothingConfigured_IsAnOverridingDeclaration() =>
        Assert.Equal(DatabaseProvider.Postgres, new PostgresFixture().ActiveProvider);

    [Fact]
    public void ActiveProvider_Configured_OverridesTheDeclaration()
    {
        Environment.SetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable, "Postgres");

        Assert.Equal(DatabaseProvider.Postgres, new SqlFixture().ActiveProvider);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("99")]
    [InlineData("SqlServer,Postgres")]
    [InlineData("Oracle")]
    public void ActiveProvider_ConfiguredWithAnythingButAName_RejectsBeforeAnyContainerIsBuilt(string value)
    {
        Environment.SetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable, value);

        Assert.Throws<InvalidOperationException>(() => new SqlFixture().ActiveProvider);
    }

    [Fact]
    public void ActiveProvider_ReadAgainAfterTheVariableChanges_ResolvedOnlyOnce()
    {
        var fixture = new SqlFixture();
        var first = fixture.ActiveProvider;

        Environment.SetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable, "Postgres");

        Assert.Equal(first, fixture.ActiveProvider);
    }

    #endregion

    #region InitializeRespawnerAsync

    [Fact]
    public async Task InitializeRespawnerAsync_NoSchemasOnPostgres_RefusesBeforeReachingTheDatabase()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new PostgresFixture().InitializeRespawnerAsync());

        Assert.Contains(nameof(SqlFixture.InitializeRespawnerAsync), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitializeRespawnerAsync_BeforeTheFixtureStarted_NamesTheStepThatWasSkipped()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SqlFixture().InitializeRespawnerAsync(["dbo"]));

        Assert.Contains(nameof(SqlFixture.InitializeAsync), error.Message, StringComparison.Ordinal);
    }

    #endregion
}
