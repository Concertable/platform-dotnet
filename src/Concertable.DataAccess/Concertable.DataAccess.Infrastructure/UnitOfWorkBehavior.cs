using System.Transactions;
using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public class UnitOfWorkBehavior<TContext>(IUnitOfWork<TContext> unitOfWork) : IUnitOfWorkBehavior<TContext>
    where TContext : DbContextBase
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        var result = await action();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        using var scope = CreateScope();
        await action();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        scope.Complete();
    }

    public async Task<T> TryExecuteAsync<T>(
        Func<Task<T>> action,
        Func<DbUpdateException, bool> isExpected,
        Func<DbUpdateException, Task<T>> onExpectedFailure,
        CancellationToken cancellationToken = default)
    {
        // Recovery belongs to whoever owns the transaction. Rolling a nested scope back dooms the caller's
        // transaction too, leaving onExpectedFailure nothing it can read or commit, so a nested failure
        // propagates to the root scope that can actually roll back and rerun.
        if (Transaction.Current is not null)
            return await ExecuteAsync(action, cancellationToken);

        DbUpdateException expected;

        // The scope must be disposed — rolling the transaction back — before onExpectedFailure runs,
        // so its reads do not join the aborted transaction.
        using (var scope = CreateScope())
        {
            try
            {
                var result = await action();
                await unitOfWork.SaveChangesAsync(cancellationToken);
                scope.Complete();
                return result;
            }
            catch (DbUpdateException exception) when (isExpected(exception))
            {
                expected = exception;
            }
        }

        return await onExpectedFailure(expected);
    }

    private static TransactionScope CreateScope() => new(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        TransactionScopeAsyncFlowOption.Enabled);
}
