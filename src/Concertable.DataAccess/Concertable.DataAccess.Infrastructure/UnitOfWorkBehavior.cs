using System.Transactions;
using Concertable.DataAccess.Application;

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

    private static TransactionScope CreateScope() => new(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        TransactionScopeAsyncFlowOption.Enabled);
}
