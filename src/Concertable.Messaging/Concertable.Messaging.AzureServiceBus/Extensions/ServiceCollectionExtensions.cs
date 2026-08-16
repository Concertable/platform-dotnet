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
        var options = new AzureServiceBusOptions();
        configure(options);
        if (string.IsNullOrWhiteSpace(options.ServiceName))
            throw new InvalidOperationException(
                "AzureServiceBusOptions.ServiceName is required — it scopes command queue names; an empty value yields a malformed 'command--<type>' queue.");

        services.Configure(configure);

        var registry = new MessageTypeRegistry();
        register(registry);
        services.AddSingleton(registry);

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
                throw new InvalidOperationException(
                    "AzureServiceBusOptions.ConnectionString is required — bind the 'asb' connection string on the host. It is validated here, on first resolution of the Service Bus client, so a host that swaps the transport in tests and never resolves the client can leave it unset.");
            return new ServiceBusClient(opts.ConnectionString);
        });

        services.AddSingleton<MessageSerializer>();
        services.AddSingleton<IBusTransport, AzureServiceBusTransport>();
        services.AddHostedService<AzureServiceBusReceiver>();

        return services;
    }
}
