using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly OutboxOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<OutboxDispatcher> logger;

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
                await DrainOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.OutboxDrainFailed(ex);
            }
            try { await Task.Delay(options.PollInterval, timeProvider, stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task DrainOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IOutboxReader>();
        var transport = scope.ServiceProvider.GetRequiredService<IBusTransport>();
        var registry = scope.ServiceProvider.GetRequiredService<MessageTypeRegistry>();
        var serializer = scope.ServiceProvider.GetRequiredService<MessageSerializer>();

        var pending = await reader.GetPendingAsync(options.BatchSize, ct);
        if (pending.Count == 0) return;

        foreach (var row in pending)
        {
            try
            {
                var type = row.Kind == MessageKind.Event
                    ? registry.ResolveEvent(row.MessageType)
                    : registry.ResolveCommand(row.MessageType);
                var instance = serializer.Deserialize(BinaryData.FromString(row.Payload), type);
                var envelope = new MessageEnvelope(
                    MessageId: row.Id,
                    MessageType: row.MessageType,
                    OccurredAtUtc: row.OccurredAtUtc,
                    CorrelationId: row.CorrelationId);

                var methodName = row.Kind == MessageKind.Event
                    ? nameof(IBusTransport.PublishAsync)
                    : nameof(IBusTransport.SendAsync);
                var method = typeof(IBusTransport).GetMethod(methodName)!.MakeGenericMethod(type);
                await (Task)method.Invoke(transport, [instance, envelope, ct])!;

                row.MarkDispatched(timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                row.RecordFailure(ex.Message, options.MaxAttempts, timeProvider.GetUtcNow());
                logger.OutboxDispatchFailed(row.MessageType, row.Id, ex);
            }
        }

        await reader.SaveChangesAsync(ct);
    }
}
