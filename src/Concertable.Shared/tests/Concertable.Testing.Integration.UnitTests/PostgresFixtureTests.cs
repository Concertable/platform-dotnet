namespace Concertable.Testing.Integration.UnitTests;

public sealed class PostgresFixtureTests
{
    #region ConnectionString

    [Fact]
    public void ConnectionString_BeforeTheFixtureStarted_NamesTheStepThatWasSkipped()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new PostgresFixture().ConnectionString);

        Assert.Contains(nameof(PostgresFixture.InitializeAsync), error.Message, StringComparison.Ordinal);
    }

    #endregion

    #region InitializeRespawnerAsync

    [Fact]
    public async Task InitializeRespawnerAsync_BeforeTheFixtureStarted_NamesTheStepThatWasSkipped()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new PostgresFixture().InitializeRespawnerAsync(["search"]));

        Assert.Contains(nameof(PostgresFixture.InitializeAsync), error.Message, StringComparison.Ordinal);
    }

    #endregion

    #region ResetAsync

    [Fact]
    public async Task ResetAsync_WithNoRespawner_NamesTheStepThatWasSkipped()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new PostgresFixture().ResetAsync());

        Assert.Contains(
            nameof(PostgresFixture.InitializeRespawnerAsync),
            error.Message,
            StringComparison.Ordinal);
    }

    #endregion
}
