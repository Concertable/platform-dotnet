using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxReader : IOutboxReader
{
    private readonly OutboxDbContext context;

    public OutboxReader(OutboxDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default) =>
        await context.Set<OutboxMessageEntity>()
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
