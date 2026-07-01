using Microsoft.Extensions.Logging;

namespace Concertable.Shared.Notification.Infrastructure;

internal static partial class Log
{
    #region SignalRNotificationClient

    [LoggerMessage(Level = LogLevel.Information, Message = "[SignalRNotificationClient] send userId={UserId} event={EventName}")]
    internal static partial void SendingSignalRNotification(this ILogger logger, string userId, string eventName);

    #endregion

    #region NotificationHub

    [LoggerMessage(Level = LogLevel.Information, Message = "[NotificationHub] connected userId={UserId} userIdentifier={UserIdentifier} connectionId={ConnectionId}")]
    internal static partial void NotificationHubConnected(this ILogger logger, string? userId, string? userIdentifier, string connectionId);

    #endregion
}
