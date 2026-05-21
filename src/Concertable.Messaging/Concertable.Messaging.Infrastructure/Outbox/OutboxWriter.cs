using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxWriter : IOutboxWriter
{
    private readonly IDbContextAccessor accessor;

    public OutboxWriter(IDbContextAccessor accessor)
    {
        this.accessor = accessor;
    }

    public Task AddAsync(OutboxMessageEntity message, CancellationToken ct = default)
    {
        var context = accessor.Context
            ?? throw new InvalidOperationException(
                "No DbContext is mid-SaveChanges; an outbox write must run inside a business "
                + "transaction. Integration events are published from pre-commit domain-event handlers.");
        context.Set<OutboxMessageEntity>().Add(message);
        return Task.CompletedTask;
    }
}
