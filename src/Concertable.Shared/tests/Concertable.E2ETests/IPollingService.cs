namespace Concertable.E2ETests;

public interface IPollingService
{
    Task UntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null);

    Task<T> UntilAsync<T>(
        Func<Task<T>> action,
        Func<T, bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null);
}
