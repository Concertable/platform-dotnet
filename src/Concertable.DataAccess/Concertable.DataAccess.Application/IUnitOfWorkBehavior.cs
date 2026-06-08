namespace Concertable.DataAccess.Application;

public interface IUnitOfWorkBehavior<TContext>
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
