using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.Messaging.Infrastructure.Inbox;

internal sealed class InboxDbContextFactory : IDesignTimeDbContextFactory<InboxDbContext>
{
    public InboxDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.B2B();
        var options = new DbContextOptionsBuilder<InboxDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new InboxDbContext(options);
    }
}
