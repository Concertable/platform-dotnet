using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

public interface IDbContextAccessor
{
    DbContext? Context { get; set; }
}
