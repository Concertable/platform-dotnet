using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Inbox;

public class InboxDbContext : DbContext
{
    public InboxDbContext(DbContextOptions<InboxDbContext> dbContextOptions)
        : base(dbContextOptions) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
}
