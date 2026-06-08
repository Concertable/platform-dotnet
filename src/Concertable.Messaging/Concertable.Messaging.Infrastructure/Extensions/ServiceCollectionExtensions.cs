using Concertable.Messaging.Contracts;
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

    public static IServiceCollection AddInMemoryTransport(this IServiceCollection services)
    {
        services.AddScoped<IBusTransport, InMemoryBusTransport>();
        return services;
    }

    public static IServiceCollection AddDirectBusKeyed(this IServiceCollection services, string key = "direct")
    {
        services.AddKeyedScoped<IBus, Bus>(key);
        return services;
    }
}
