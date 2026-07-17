using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseSqlServer(DesignTimeConfiguration.ConnectionString())
            .Options;
        return new OutboxDbContext(options, Options.Create(new OutboxOptions()));
    }
}
