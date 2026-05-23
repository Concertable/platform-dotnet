using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Concertable.Messaging.AzureServiceBus;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed processing event {MessageType}")]
    internal static partial void FailedProcessingEvent(this ILogger logger, object? messageType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed processing command {MessageType}")]
    internal static partial void FailedProcessingCommand(this ILogger logger, object? messageType, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Service Bus processor error on {EntityPath} ({Source})")]
    internal static partial void ServiceBusProcessorError(this ILogger logger, string entityPath, ServiceBusErrorSource source, Exception ex);
}
