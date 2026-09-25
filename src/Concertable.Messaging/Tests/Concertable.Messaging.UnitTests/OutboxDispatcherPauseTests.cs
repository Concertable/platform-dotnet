using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.UnitTests;

public sealed class OutboxDispatcherPauseTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly GatedOutboxReader reader = new();
    private readonly OutboxDispatcher dispatcher;

    public OutboxDispatcherPauseTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxReader>(reader);
        services.AddSingleton<IMessageDispatchResolver, UnusedDispatchResolver>();
        services.AddSingleton<MessageSerializer>();
        var provider = services.BuildServiceProvider();

        var options = Options.Create(new OutboxOptions { PollInterval = TimeSpan.FromMilliseconds(20) });
        this.dispatcher = new OutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            TimeProvider.System,
            NullLogger<OutboxDispatcher>.Instance);
    }

    [Fact]
    public async Task PauseAsync_WhileADrainIsInFlight_WaitsForItToFinish()
    {
        await dispatcher.StartAsync(CancellationToken.None);
        await reader.DrainStarted.WaitAsync(Timeout);

        var pause = dispatcher.PauseAsync();
        await Task.Delay(100);
        Assert.False(pause.IsCompleted);

        reader.ReleaseDrain();
        await pause.WaitAsync(Timeout);

        await dispatcher.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task WhilePaused_TheLoopDoesNotDrain_AndResumeLetsItDrainAgain()
    {
        await dispatcher.StartAsync(CancellationToken.None);
        await reader.DrainStarted.WaitAsync(Timeout);
        reader.ReleaseDrain();

        await dispatcher.PauseAsync();
        var pausedCount = reader.Calls;
        await Task.Delay(300);
        Assert.Equal(pausedCount, reader.Calls);

        await dispatcher.ResumeAsync();
        await WaitUntilAsync(() => reader.Calls > pausedCount);

        await dispatcher.StopAsync(CancellationToken.None);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!condition())
            await Task.Delay(20, cts.Token);
    }

    private sealed class GatedOutboxReader : IOutboxReader
    {
        private readonly TaskCompletionSource drainStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int calls;

        public Task DrainStarted => drainStarted.Task;

        public int Calls => Volatile.Read(ref calls);

        public void ReleaseDrain() => release.TrySetResult();

        public async Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default)
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                drainStarted.TrySetResult();
                await release.Task.WaitAsync(ct);
            }
            return [];
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class UnusedDispatchResolver : IMessageDispatchResolver
    {
        public IMessageDispatcher Resolve(MessageKind kind) => throw new NotSupportedException();
    }
}
