using System.Transactions;
using Concertable.DataAccess;

namespace Concertable.DataAccess.Infrastructure;

public class UnitOfWorkBehavior<TContext>(IUnitOfWork<TContext> unitOfWork) : IUnitOfWorkBehavior<TContext>
    where TContext : DbContextBase
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        var result = await action();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        await action();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        scope.Complete();
    }
}
