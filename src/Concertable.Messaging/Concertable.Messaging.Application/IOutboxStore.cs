using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Application;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessageEntity message, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
