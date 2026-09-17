using Concertable.Messaging.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.IntegrationTests;

[Collection(MessageStoreCollection.Name)]
public sealed class MessageStoreMigrationTests
{
    private readonly MessageStoreFixture fixture;

    public MessageStoreMigrationTests(MessageStoreFixture fixture)
    {
        this.fixture = fixture;
    }

    #region MigrateAsync

    [Fact]
    public async Task MigrateAsync_RunAgainOverAnAlreadyMigratedDatabase_AppliesNothingFurther()
    {
        await using var inbox = fixture.CreateInboxContext();
        await using var outbox = fixture.CreateOutboxContext();

        await inbox.Database.MigrateAsync();
        await outbox.Database.MigrateAsync();

        Assert.Empty(await inbox.Database.GetPendingMigrationsAsync());
        Assert.Empty(await outbox.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task MigrateAsync_EachStore_RecordsItsHistoryInTheMessagingSchema()
    {
        await using var inbox = fixture.CreateInboxContext();
        await using var outbox = fixture.CreateOutboxContext();

        Assert.Single(await inbox.Database.GetAppliedMigrationsAsync());
        Assert.Single(await outbox.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task MigrateAsync_TheOutboxTable_LandsInTheConfiguredSchema()
    {
        await using var outbox = fixture.CreateOutboxContext();

        var schema = await outbox.Database
            .SqlQuery<string>($"""SELECT table_schema AS "Value" FROM information_schema.tables WHERE table_name = {Schema.Tables.Outbox}""")
            .SingleAsync();

        Assert.Equal(Schema.Name, schema);
    }

    #endregion
}
