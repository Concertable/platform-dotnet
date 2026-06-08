using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Concertable.Messaging.AzureServiceBus;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed processing event {MessageType}")]
    internal static partial void FailedProcessingEvent(this ILogger logger, object? messageType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed processing command {MessageType}")]
    internal static partial void FailedProcessingCommand(this ILogger logger, object? messageType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dead-lettering undeserializable event {MessageType}")]
    internal static partial void DeadLetteringEvent(this ILogger logger, object? messageType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dead-lettering undeserializable command {MessageType}")]
    internal static partial void DeadLetteringCommand(this ILogger logger, object? messageType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dead-lettering message with invalid MessageId '{MessageId}'")]
    internal static partial void DeadLetteringInvalidMessageId(this ILogger logger, string? messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Service Bus processor error on {EntityPath} ({Source})")]
    internal static partial void ServiceBusProcessorError(this ILogger logger, string entityPath, ServiceBusErrorSource source, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Service Bus event processor started on {Topic}/{Subscription}")]
    internal static partial void EventProcessorStarted(this ILogger logger, string topic, string subscription);

    [LoggerMessage(Level = LogLevel.Information, Message = "Service Bus command processor started on {Queue}")]
    internal static partial void CommandProcessorStarted(this ILogger logger, string queue);

    [LoggerMessage(Level = LogLevel.Information, Message = "Service Bus received {MessageType} on {EntityPath}")]
    internal static partial void MessageReceived(this ILogger logger, object? messageType, string entityPath);
}
