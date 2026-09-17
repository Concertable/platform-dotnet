using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.IntegrationTests;

public sealed class MessageStoreFixture : IAsyncLifetime
{
    private const string InboxHistory = $"{OwnedSchemaSelector.MigrationsHistory}_Inbox";
    private const string OutboxHistory = $"{OwnedSchemaSelector.MigrationsHistory}_Outbox";

    private readonly PostgresFixture postgres = new();

    public string ConnectionString => postgres.ConnectionString;

    public async Task InitializeAsync()
    {
        await postgres.InitializeAsync();

        await using (var inbox = CreateInboxContext())
            await inbox.Database.MigrateAsync();
        await using (var outbox = CreateOutboxContext())
            await outbox.Database.MigrateAsync();

        await postgres.InitializeRespawnerAsync([Schema.Name]);
    }

    public Task ResetAsync() => postgres.ResetAsync();

    public Task DisposeAsync() => postgres.DisposeAsync();

    public InboxDbContext CreateInboxContext() =>
        new(new DbContextOptionsBuilder<InboxDbContext>()
            .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable(InboxHistory, Schema.Name))
            .Options);

    public OutboxDbContext CreateOutboxContext(OutboxOptions? options = null) =>
        new(new DbContextOptionsBuilder<OutboxDbContext>()
                .UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable(OutboxHistory, Schema.Name))
                .Options,
            Options.Create(options ?? new OutboxOptions()));
}

[CollectionDefinition(Name)]
public sealed class MessageStoreCollection : ICollectionFixture<MessageStoreFixture>
{
    public const string Name = "MessageStore";
}
