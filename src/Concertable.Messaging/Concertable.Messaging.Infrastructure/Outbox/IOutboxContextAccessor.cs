using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

public interface IOutboxContextAccessor
{
    DbContext? Current { get; set; }
}
