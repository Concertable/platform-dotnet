using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Application;

public interface IOutboxReader
{
    Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
