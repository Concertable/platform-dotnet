using Azure.Messaging.ServiceBus;
using Concertable.Messaging.Application;
using Concertable.Messaging.AzureServiceBus.Options;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AzureServiceBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusTransport(
        this IServiceCollection services,
        Action<AzureServiceBusOptions> configure,
        Action<MessageTypeRegistry> register)
    {
        services.Configure(configure);

        var registry = new MessageTypeRegistry();
        register(registry);
        services.AddSingleton(registry);

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            return new ServiceBusClient(opts.ConnectionString);
        });

        services.AddSingleton<MessageSerializer>();
        services.AddSingleton<IBusTransport, AzureServiceBusTransport>();
        services.AddHostedService<AzureServiceBusReceiver>();

        return services;
    }
}
