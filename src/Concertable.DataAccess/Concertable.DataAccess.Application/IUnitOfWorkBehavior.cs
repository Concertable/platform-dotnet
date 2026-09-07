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
    /// Runs <paramref name="action"/> in the ambient transaction. A <see cref="DbUpdateException"/> that
    /// <paramref name="isExpected"/> accepts rolls the scope back — every enlisted context with it — and
    /// <paramref name="onExpectedFailure"/> then produces the outcome, after the scope has been disposed.
    /// Every other failure, and cancellation, propagates. Nested inside another scope this classifies
    /// nothing, so the failure reaches the root scope that can roll back.
    /// </summary>
    Task<T> TryExecuteAsync<T>(
        Func<Task<T>> action,
        Func<DbUpdateException, bool> isExpected,
        Func<DbUpdateException, Task<T>> onExpectedFailure,
        CancellationToken cancellationToken = default);
}
