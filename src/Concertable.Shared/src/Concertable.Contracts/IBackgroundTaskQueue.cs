namespace Concertable.Contracts;

public interface IBackgroundTaskQueue
{
    Task EnqueueAsync(Func<CancellationToken, Task> workItem);
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
