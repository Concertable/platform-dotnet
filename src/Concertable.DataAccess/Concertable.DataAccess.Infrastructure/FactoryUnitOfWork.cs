using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public class FactoryUnitOfWork<TContext>(IDbContextFactory<TContext> dbContextFactory)
    : IUnitOfWorkBoundary<TContext>
    where TContext : DbContextBase
{
    public Task ExecuteAsync(
        Func<TContext, Task> operation,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async context =>
        {
            await operation(context);
            return true;
        }, cancellationToken);

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<TContext, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var unitOfWork = new UnitOfWork<TContext>(context);

        return await unitOfWork.ExecuteAsync(
            () => operation(context),
            cancellationToken);
    }
}