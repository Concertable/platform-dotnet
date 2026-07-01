using Concertable.Contracts;
using System.Threading.Channels;

namespace Concertable.Kernel.Background;

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> queue;

    public BackgroundTaskQueue(int capacity = 100)
    {
        queue = Channel.CreateBounded<Func<CancellationToken, Task>>(
            new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait });
    }

    public async Task EnqueueAsync(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await queue.Writer.WriteAsync(workItem);
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken) =>
        await queue.Reader.ReadAsync(cancellationToken);
}
