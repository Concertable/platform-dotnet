using Concertable.Kernel.Identity;
using Concertable.Shared.Notification.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Concertable.Shared.Notification.Infrastructure.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        this.logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.User?.GetId();

        logger.NotificationHubConnected(userId, Context.UserIdentifier, Context.ConnectionId);

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.User?.GetId();

        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

        await base.OnDisconnectedAsync(exception);
    }
}
