using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Application;

public interface IUnitOfWorkBoundary<TContext>
{
    Task ExecuteAsync(
        Func<TContext, Task> operation,
        CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        Func<TContext, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> in its own context and transaction. When the commit fails with a
    /// <see cref="DbUpdateException"/> that <paramref name="isExpected"/> accepts, the transaction is
    /// rolled back and <paramref name="onExpectedFailure"/> produces the outcome, after that context has
    /// been disposed — so a re-read there sees committed truth rather than the failed unit of work. Every
    /// other failure, and cancellation, propagates.
    /// </summary>
    Task<TResult> TryExecuteAsync<TResult>(
        Func<TContext, Task<TResult>> operation,
        Func<DbUpdateException, bool> isExpected,
        Func<DbUpdateException, Task<TResult>> onExpectedFailure,
        CancellationToken cancellationToken = default);
}