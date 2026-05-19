using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddScoped<IBus, Bus>();
        services.AddScoped<IBusTransport, InMemoryBusTransport>();
        return services;
    }
}
