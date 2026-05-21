using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class DbContextAccessor : IDbContextAccessor
{
    public DbContext? Context { get; set; }
}
