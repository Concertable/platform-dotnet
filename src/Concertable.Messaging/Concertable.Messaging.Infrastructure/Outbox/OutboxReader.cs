using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxReader : IOutboxReader
{
    private readonly OutboxDbContext context;
    private readonly TimeProvider timeProvider;

    public OutboxReader(OutboxDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        return await context.Set<OutboxMessageEntity>()
            .Where(m => m.Status == OutboxStatus.Pending
                     && (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
            .OrderBy(m => m.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
