using Concertable.DataAccess.Application;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.DataAccess.Infrastructure;

public class OutboxUnitOfWorkBehavior<TContext> : IOutboxUnitOfWorkBehavior<TContext>
    where TContext : DbContextBase
{
    private readonly TContext context;
    private readonly IDbContextAccessor accessor;

    public OutboxUnitOfWorkBehavior(TContext context, IDbContextAccessor accessor)
    {
        this.context = context;
        this.accessor = accessor;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var previous = accessor.Context;
        accessor.Context = context;
        try
        {
            var result = await action();
            await context.SaveChangesAsync(cancellationToken);
            return result;
        }
        finally
        {
            accessor.Context = previous;
        }
    }

    public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => { await action(); return true; }, cancellationToken);
}
