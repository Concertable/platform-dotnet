using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.Application;

/// <summary>
/// A unit of work over a single <typeparamref name="TContext"/>: commit tracked changes with
/// <see cref="SaveChangesAsync"/>, or run a block and commit it atomically with
/// <see cref="ExecuteAsync(System.Func{System.Threading.Tasks.Task},System.Threading.CancellationToken)"/>
/// — one transaction, rolled back on throw. Use for any write confined to one module's context. To commit
/// a block spanning several modules' contexts atomically, use <see cref="IUnitOfWorkBehavior{TContext}"/> instead.
/// </summary>
public interface IUnitOfWork<TContext>
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> and commits it atomically on this single <typeparamref name="TContext"/>
    /// (one transaction; a throw rolls it back). The default. Keep external side effects (HTTP calls, message
    /// publishes) outside the delegate. To span several modules' contexts, use <see cref="IUnitOfWorkBehavior{TContext}"/>.
    /// </summary>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteAsync(System.Func{System.Threading.Tasks.Task},System.Threading.CancellationToken)"/>
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
}
