using Azure.Messaging.ServiceBus;
using Concertable.Messaging.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AzureServiceBus;

internal sealed class AzureServiceBusReceiver : BackgroundService
{
    private readonly ServiceBusClient client;
    private readonly AzureServiceBusOptions options;
    private readonly MessageTypeRegistry registry;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly MessageSerializer serializer;
    private readonly ILogger<AzureServiceBusReceiver> logger;
    private readonly List<ServiceBusProcessor> processors = new();

    public AzureServiceBusReceiver(
        ServiceBusClient client,
        IOptions<AzureServiceBusOptions> options,
        MessageTypeRegistry registry,
        IServiceScopeFactory scopeFactory,
        MessageSerializer serializer,
        ILogger<AzureServiceBusReceiver> logger)
    {
        this.client = client;
        this.options = options.Value;
        this.registry = registry;
        this.scopeFactory = scopeFactory;
        this.serializer = serializer;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var eventType in registry.RegisteredEventTypes)
        {
            var topic = options.TopicNameFor(eventType);
            var processor = client.CreateProcessor(topic, options.ServiceName);
            processor.ProcessMessageAsync += args => HandleEventAsync(args, eventType);
            processor.ProcessErrorAsync += HandleErrorAsync;
            processors.Add(processor);
            await processor.StartProcessingAsync(stoppingToken);
        }

        foreach (var commandType in registry.RegisteredCommandTypes)
        {
            var queue = options.QueueNameFor(commandType);
            var processor = client.CreateProcessor(queue);
            processor.ProcessMessageAsync += args => HandleCommandAsync(args, commandType);
            processor.ProcessErrorAsync += HandleErrorAsync;
            processors.Add(processor);
            await processor.StartProcessingAsync(stoppingToken);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException) { }

        foreach (var processor in processors)
            await processor.DisposeAsync();
    }

    private async Task HandleEventAsync(ProcessMessageEventArgs args, Type eventType)
    {
        try
        {
            var @event = serializer.Deserialize(args.Message.Body, eventType);
            var envelope = new MessageEnvelope(
                Guid.Parse(args.Message.MessageId),
                args.Message.ApplicationProperties.GetValueOrDefault("MessageType")?.ToString() ?? eventType.FullName!,
                args.Message.EnqueuedTime,
                args.Message.CorrelationId);
            using var scope = scopeFactory.CreateScope();
            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;
            foreach (var handler in scope.ServiceProvider.GetServices(handlerType))
            {
                if (handler is null) continue;
                await (Task)method.Invoke(handler, [@event, envelope, args.CancellationToken])!;
            }
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing event {MessageType}",
                args.Message.ApplicationProperties.GetValueOrDefault("MessageType"));
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private async Task HandleCommandAsync(ProcessMessageEventArgs args, Type commandType)
    {
        try
        {
            var command = serializer.Deserialize(args.Message.Body, commandType);
            var envelope = new MessageEnvelope(
                Guid.Parse(args.Message.MessageId),
                args.Message.ApplicationProperties.GetValueOrDefault("MessageType")?.ToString() ?? commandType.FullName!,
                args.Message.EnqueuedTime,
                args.Message.CorrelationId);
            using var scope = scopeFactory.CreateScope();
            var handlerType = typeof(IIntegrationCommandHandler<>).MakeGenericType(commandType);
            var handlers = scope.ServiceProvider.GetServices(handlerType).Where(h => h is not null).ToList();
            if (handlers.Count == 0)
                throw new InvalidOperationException(
                    $"No handler registered for command {commandType.FullName}. Commands require exactly one handler.");
            if (handlers.Count > 1)
                throw new InvalidOperationException(
                    $"Multiple handlers registered for command {commandType.FullName}. Commands require exactly one handler.");

            var method = handlerType.GetMethod(nameof(IIntegrationCommandHandler<IIntegrationCommand>.HandleAsync))!;
            await (Task)method.Invoke(handlers[0], [command, envelope, args.CancellationToken])!;
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing command {MessageType}",
                args.Message.ApplicationProperties.GetValueOrDefault("MessageType"));
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception,
            "Service Bus processor error on {EntityPath} ({Source})",
            args.EntityPath, args.ErrorSource);
        return Task.CompletedTask;
    }
}
