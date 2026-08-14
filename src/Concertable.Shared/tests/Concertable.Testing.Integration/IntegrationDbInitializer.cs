using Concertable.DataAccess.Application;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Testing.Integration;

public sealed class IntegrationDbInitializer : IDbInitializer
{
    private readonly IEnumerable<ITestSeeder> seeders;
    private readonly InboxDbContext inboxDbContext;
    private readonly OutboxDbContext outboxDbContext;
    private readonly SeedingScope seedingScope;

    public IntegrationDbInitializer(
        IEnumerable<ITestSeeder> seeders,
        InboxDbContext inboxDbContext,
        OutboxDbContext outboxDbContext,
        SeedingScope seedingScope)
    {
        this.seeders = seeders;
        this.inboxDbContext = inboxDbContext;
        this.outboxDbContext = outboxDbContext;
        this.seedingScope = seedingScope;
    }

    public async Task InitializeAsync()
    {
        await inboxDbContext.Database.MigrateAsync();
        await outboxDbContext.Database.MigrateAsync();

        foreach (var seeder in seeders.OrderBy(s => s.Order))
            await seeder.MigrateAsync();

        using (seedingScope.Activate())
        {
            foreach (var seeder in seeders.OrderBy(s => s.Order))
                await seeder.SeedAsync();
        }
    }
}
