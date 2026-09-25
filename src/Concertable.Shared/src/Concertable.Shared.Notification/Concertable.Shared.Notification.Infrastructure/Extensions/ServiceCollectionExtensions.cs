using System.Text.Json;
using System.Text.Json.Serialization;
using Concertable.Kernel.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Shared.Notification.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddNotificationClient()
        {
            services.AddSignalR()
                .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
            services.AddSingleton<INotificationClient, SignalRNotificationClient>();
            return services;
        }
    }
}
