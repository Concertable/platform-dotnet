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

    public async Task<TResult> TryExecuteAsync<TResult>(
        Func<TContext, Task<TResult>> operation,
        Func<DbUpdateException, bool> isExpected,
        Func<DbUpdateException, Task<TResult>> onExpectedFailure,
        CancellationToken cancellationToken = default)
    {
        DbUpdateException expected;

        // The context must be disposed — and its transaction rolled back — before onExpectedFailure
        // runs, so its re-read sees committed truth rather than the failed unit of work.
        await using (var context = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            var unitOfWork = new UnitOfWork<TContext>(context);
            try
            {
                return await unitOfWork.ExecuteAsync(() => operation(context), cancellationToken);
            }
            catch (DbUpdateException exception) when (isExpected(exception))
            {
                expected = exception;
            }
        }

        return await onExpectedFailure(expected);
    }
}