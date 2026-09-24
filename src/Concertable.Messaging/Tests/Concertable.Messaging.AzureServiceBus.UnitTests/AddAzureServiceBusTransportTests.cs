using Azure.Messaging.ServiceBus;
using Concertable.Messaging.AzureServiceBus.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.Messaging.AzureServiceBus.UnitTests;

public sealed class AddAzureServiceBusTransportTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=x;SharedAccessKey=y";

    [Fact]
    public void AddAzureServiceBusTransport_WithoutConnectionString_DoesNotThrowAtRegistration()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddAzureServiceBusTransport(opts => opts.ServiceName = "b2b", _ => { }));

        Assert.Null(exception);
    }

    [Fact]
    public void AddAzureServiceBusTransport_WithoutServiceName_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddAzureServiceBusTransport(_ => { }, _ => { }));
    }

    [Fact]
    public void ServiceBusClient_WhenConnectionStringMissing_ThrowsOnResolution()
    {
        var provider = new ServiceCollection()
            .AddAzureServiceBusTransport(opts => opts.ServiceName = "b2b", _ => { })
            .BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ServiceBusClient>());
    }

    [Fact]
    public void ServiceBusClient_WhenConnectionStringPresent_ResolvesClient()
    {
        var provider = new ServiceCollection()
            .AddAzureServiceBusTransport(
                opts =>
                {
                    opts.ServiceName = "b2b";
                    opts.ConnectionString = FakeConnectionString;
                },
                _ => { })
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<ServiceBusClient>());
    }

    [Fact]
    public void IngressQuiescer_ResolvesTheSameInstanceAsTheHostedReceiver()
    {
        var provider = new ServiceCollection()
            .AddAzureServiceBusTransport(
                opts =>
                {
                    opts.ServiceName = "b2b";
                    opts.ConnectionString = FakeConnectionString;
                },
                _ => { })
            .AddLogging()
            .BuildServiceProvider();

        var quiescer = provider.GetRequiredService<IIngressQuiescer>();
        var hosted = provider.GetServices<IHostedService>().Single();

        Assert.Same(hosted, quiescer);
    }

    [Fact]
    public void Receiver_IsRegisteredWithAnImplementationType_SoAHostCanRemoveIt()
    {
        var services = new ServiceCollection()
            .AddAzureServiceBusTransport(opts => opts.ServiceName = "b2b", _ => { });

        var receiver = services.Single(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType?.Name == "AzureServiceBusReceiver");

        Assert.NotNull(receiver.ImplementationType);
    }
}
