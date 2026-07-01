using Concertable.Kernel.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Notification.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationClient(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<INotificationClient, SignalRNotificationClient>();
        return services;
    }
}
