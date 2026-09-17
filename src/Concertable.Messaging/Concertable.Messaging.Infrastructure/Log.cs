using Microsoft.Extensions.Logging;

namespace Concertable.Messaging.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox drain failed")]
    internal static partial void OutboxDrainFailed(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox dispatch failed for {MessageType} {MessageId}")]
    internal static partial void OutboxDispatchFailed(this ILogger logger, string messageType, Guid messageId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox completion lost for {Count} message(s) another worker had already reclaimed")]
    internal static partial void OutboxCompletionLost(this ILogger logger, int count);
}
