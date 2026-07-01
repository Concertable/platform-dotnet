using Concertable.Kernel.Notifications;
using Concertable.Shared.Notification.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Concertable.Shared.Notification.Infrastructure;

internal sealed class SignalRNotificationClient : INotificationClient
{
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly ILogger<SignalRNotificationClient> logger;

    public SignalRNotificationClient(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationClient> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public Task SendAsync(string userId, string eventName, object payload)
    {
        logger.SendingSignalRNotification(userId, eventName);
        return hubContext.Clients.Group(userId).SendAsync(eventName, payload);
    }
}
