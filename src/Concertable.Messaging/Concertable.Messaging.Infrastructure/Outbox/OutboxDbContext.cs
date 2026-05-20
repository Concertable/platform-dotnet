using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

public class OutboxDbContext : DbContext
{
    private readonly IOptions<OutboxOptions> options;

    public OutboxDbContext(DbContextOptions<OutboxDbContext> dbContextOptions, IOptions<OutboxOptions> options)
        : base(dbContextOptions)
    {
        this.options = options;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration(options));
    }
}
