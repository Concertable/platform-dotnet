using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Application;

/// <summary>
/// Runs a block that writes across multiple module <c>DbContext</c>s within one service and commits it
/// atomically, via an ambient <see cref="System.Transactions.TransactionScope"/> so every context's
/// SaveChanges enlists in the one transaction (e.g. create a Deal and an Opportunity together). Use ONLY
/// for cross-module writes; for a single context use <see cref="IUnitOfWork{TContext}"/>. Never span
/// services — coordinate those with messages, not a transaction.
/// </summary>
public interface IUnitOfWorkBehavior<TContext>
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="action"/> in the ambient transaction. When the write fails with a
    /// <see cref="DbUpdateException"/> that <paramref name="isExpected"/> accepts, the transaction is
    /// rolled back — including every other context enlisted in it — and
    /// <paramref name="onExpectedFailure"/> then produces the outcome, after the scope has been
    /// disposed. Every other failure, and cancellation, propagates.
    /// <para>
    /// Use this, never a <c>TrySaveChangesAsync</c> inside
    /// <see cref="ExecuteAsync{T}"/>: a block that returns normally commits the ambient transaction, so a
    /// failure classified inside it still commits whatever that save's pre-commit handlers wrote to other
    /// contexts.
    /// </para>
    /// </summary>
    Task<T> TryExecuteAsync<T>(
        Func<Task<T>> action,
        Func<DbUpdateException, bool> isExpected,
        Func<DbUpdateException, Task<T>> onExpectedFailure,
        CancellationToken cancellationToken = default);
}
