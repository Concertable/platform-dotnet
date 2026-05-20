using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxStore<TContext> : IOutboxStore where TContext : DbContext
{
    private readonly TContext context;

    public OutboxStore(TContext context)
    {
        this.context = context;
    }

    public Task AddAsync(OutboxMessageEntity message, CancellationToken ct = default)
    {
        context.Set<OutboxMessageEntity>().Add(message);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default) =>
        await context.Set<OutboxMessageEntity>()
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
