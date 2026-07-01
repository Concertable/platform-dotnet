using Concertable.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Kernel.Background;

public sealed class BackgroundTaskRunner : IBackgroundTaskRunner
{
    private readonly IBackgroundTaskQueue queue;
    private readonly IServiceScopeFactory scopeFactory;

    public BackgroundTaskRunner(IBackgroundTaskQueue queue, IServiceScopeFactory scopeFactory)
    {
        this.queue = queue;
        this.scopeFactory = scopeFactory;
    }

    public Task RunAsync(Func<CancellationToken, Task> work) => queue.EnqueueAsync(work);

    public Task RunAsync<TService>(Func<TService, CancellationToken, Task> work) where TService : notnull
    {
        return queue.EnqueueAsync(async ct =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            await work(service, ct);
        });
    }
}
