using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxDispatcher : BackgroundService, IIngressQuiescer
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly OutboxOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OutboxDispatcher> logger;
    private readonly SemaphoreSlim drainGate = new(1, 1);
    private volatile bool paused;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        TimeProvider timeProvider,
        ILogger<OutboxDispatcher> logger)
    {
        this.scopeFactory = scopeFactory;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await drainGate.WaitAsync(stoppingToken);
                try
                {
                    if (!paused)
                        await DrainOnceAsync(stoppingToken);
                }
                finally
                {
                    drainGate.Release();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.OutboxDrainFailed(ex);
            }
            try { await Task.Delay(options.PollInterval, timeProvider, stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }

    /// <summary>Acquiring the gate waits out a drain already running, and <see cref="paused"/> then keeps the
    /// loop from starting another under that same gate — so once this returns no drain touches the database
    /// until <see cref="ResumeAsync"/>.</summary>
    public async Task PauseAsync(CancellationToken ct = default)
    {
        await drainGate.WaitAsync(ct);
        try
        {
            paused = true;
        }
        finally
        {
            drainGate.Release();
        }
    }

    public Task ResumeAsync(CancellationToken ct = default)
    {
        paused = false;
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        drainGate.Dispose();
        base.Dispose();
    }

    internal async Task DrainOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IOutboxReader>();
        var resolver = scope.ServiceProvider.GetRequiredService<IMessageDispatchResolver>();
        var serializer = scope.ServiceProvider.GetRequiredService<MessageSerializer>();

        var pending = await reader.GetPendingAsync(options.BatchSize, ct);
        if (pending.Count == 0) return;

        foreach (var row in pending)
        {
            try
            {
                var dispatcher = resolver.Resolve(row.Kind);
                var type = dispatcher.ResolveType(row.MessageType);
                var instance = serializer.Deserialize(BinaryData.FromString(row.Payload), type);
                await dispatcher.DispatchAsync(instance, row.ToEnvelope(), ct);

                row.MarkDispatched(timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                row.RecordFailure(ex.Message, options.MaxAttempts, timeProvider.GetUtcNow());
                logger.OutboxDispatchFailed(row.MessageType, row.Id, ex);
            }
        }

        await SaveOutcomesAsync(reader, ct);
    }

    /// <summary>The claim leases a row, and the lease is a concurrency predicate on its completion, so a
    /// worker whose lease expired mid-drain must lose only its own rows. Saved as one batch, the first
    /// conflict would roll back every other row's outcome and leave the whole batch to be redelivered.</summary>
    private async Task SaveOutcomesAsync(IOutboxReader reader, CancellationToken ct)
    {
        while (true)
        {
            try
            {
                await reader.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException ex) when (ex.Entries.Count > 0)
            {
                foreach (var entry in ex.Entries)
                    entry.State = EntityState.Detached;
                logger.OutboxCompletionLost(ex.Entries.Count);
            }
        }
    }
}
