using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.Messaging.Infrastructure.Inbox;

internal sealed class InboxDbContextFactory : IDesignTimeDbContextFactory<InboxDbContext>
{
    public InboxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InboxDbContext>()
            .UseSqlServer(DesignTimeConfiguration.ConnectionString())
            .Options;
        return new InboxDbContext(options);
    }
}
