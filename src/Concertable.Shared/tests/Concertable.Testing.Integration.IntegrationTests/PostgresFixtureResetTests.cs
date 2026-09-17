namespace Concertable.Testing.Integration.IntegrationTests;

[Collection(ResetScopeCollection.Name)]
public sealed class PostgresFixtureResetTests
{
    private readonly ResetScopeFixture fixture;

    public PostgresFixtureResetTests(ResetScopeFixture fixture)
    {
        this.fixture = fixture;
    }

    #region ResetAsync

    [Fact]
    public async Task ResetAsync_TwoCyclesOverAnOwnedSchema_EmptiesItEachTime()
    {
        foreach (var cycle in new[] { "first", "second" })
        {
            await fixture.SeedSentinelsAsync($"{cycle}-a", $"{cycle}-b");
            Assert.Equal(2, await SentinelCountAsync());

            await fixture.ResetAsync();

            Assert.Equal(0, await SentinelCountAsync());
        }
    }

    [Fact]
    public async Task ResetAsync_AnOwnedSchemasMigrationHistory_IsLeftStanding()
    {
        await fixture.SeedSentinelsAsync("kept-history");

        await fixture.ResetAsync();

        Assert.Equal(1, await fixture.ScalarAsync(
            $"""SELECT count(*) FROM "{ResetScopeFixture.OwnedSchema}"."{OwnedSchemaSelector.MigrationsHistory}" """));
    }

    [Fact]
    public async Task ResetAsync_TheSharedMessageStoreHistories_AreLeftStanding()
    {
        await fixture.SeedSentinelsAsync("kept-message-histories");

        await fixture.ResetAsync();

        Assert.Equal(1, await fixture.ScalarAsync(
            $"""SELECT count(*) FROM "{ResetScopeFixture.MessagingSchema}"."{OwnedSchemaSelector.MigrationsHistory}_Inbox" """));
        Assert.Equal(1, await fixture.ScalarAsync(
            $"""SELECT count(*) FROM "{ResetScopeFixture.MessagingSchema}"."{OwnedSchemaSelector.MigrationsHistory}_Outbox" """));
    }

    [Fact]
    public async Task ResetAsync_WhatPostgisInstalled_IsLeftStanding()
    {
        await fixture.SeedSentinelsAsync("kept-postgis");

        await fixture.ResetAsync();

        Assert.True(await fixture.ScalarAsync("SELECT count(*) FROM public.spatial_ref_sys") > 0);
    }

    [Fact]
    public async Task ResetAsync_AnIdentityColumn_StartsCountingAgain()
    {
        await fixture.SeedSentinelsAsync("before-a", "before-b");
        await fixture.ResetAsync();

        await fixture.SeedSentinelsAsync("after");

        Assert.Equal(1, await fixture.ScalarAsync(
            $"""SELECT min("Id") FROM "{ResetScopeFixture.OwnedSchema}"."Sentinel" """));
    }

    #endregion

    private Task<long> SentinelCountAsync() =>
        fixture.ScalarAsync($"""SELECT count(*) FROM "{ResetScopeFixture.OwnedSchema}"."Sentinel" """);
}
