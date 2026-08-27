namespace Concertable.DataAccess.Application;

public interface IUnitOfWorkBoundary<TContext>
{
    Task ExecuteAsync(
        Func<TContext, Task> operation,
        CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        Func<TContext, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}