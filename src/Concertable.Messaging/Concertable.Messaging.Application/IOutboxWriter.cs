using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Application;

public interface IOutboxWriter
{
    Task AddAsync(OutboxMessageEntity message, CancellationToken ct = default);
}
