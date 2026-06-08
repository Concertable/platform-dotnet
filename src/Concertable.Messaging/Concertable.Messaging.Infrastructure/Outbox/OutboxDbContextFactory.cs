using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxDbContextFactory : IDesignTimeDbContextFactory<OutboxDbContext>
{
    public OutboxDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
            ?? "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new OutboxDbContext(options, Options.Create(new OutboxOptions()));
    }
}
