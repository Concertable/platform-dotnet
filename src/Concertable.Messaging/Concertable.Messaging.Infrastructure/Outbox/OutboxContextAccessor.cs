using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxContextAccessor : IOutboxContextAccessor
{
    public DbContext? Current { get; set; }
}
