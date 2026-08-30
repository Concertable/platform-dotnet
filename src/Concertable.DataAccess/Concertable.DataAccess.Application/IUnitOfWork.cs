using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.Application;

/// <summary>
/// A unit of work over a single <typeparamref name="TContext"/>. Commit one staged change set with
/// <see cref="SaveChangesAsync"/>, or use
/// <see cref="ExecuteAsync(System.Func{System.Threading.Tasks.Task},System.Threading.CancellationToken)"/>
/// when one operation requires several saves in one transaction. To commit a block spanning several
/// modules' contexts atomically, use <see cref="IUnitOfWorkBehavior{TContext}"/> instead.
/// </summary>
public interface IUnitOfWork<TContext>
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves pending changes. Returns <see langword="false"/> after an EF update failure and clears the
    /// complete tracked unit of work; every other failure propagates.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> atomically on this single <typeparamref name="TContext"/> when it
    /// requires several saves or transactional reads. Keep external side effects (HTTP calls, message publishes)
    /// outside the delegate. For one staged change set, use <see cref="SaveChangesAsync"/>. To span several
    /// modules' contexts, use <see cref="IUnitOfWorkBehavior{TContext}"/>.
    /// </summary>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteAsync(System.Func{System.Threading.Tasks.Task},System.Threading.CancellationToken)"/>
    Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
}
