namespace Concertable.Kernel.Notifications;

public interface INotificationClient
{
    Task SendAsync(string userId, string eventName, object payload);
}
