using Concertable.Messaging.Contracts;
using Concertable.Testing.Integration.Logging;
using Concertable.Testing.Integration.Mocks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Concertable.Testing.Integration;

/// <summary>
/// The <c>ConfigureTestServices</c> steps every service integration fixture shares. Compose the ones a
/// fixture needs — a service with no Azure Service Bus (e.g. Search) simply omits
/// <see cref="RemoveAzureServiceBus"/>.
/// </summary>
public static class IntegrationTestHostExtensions
{
    private const string AzureServiceBusReceiverTypeName = "AzureServiceBusReceiver";

    /// <summary>Routes host logs to the current xunit test output through <paramref name="output"/>.</summary>
    public static IServiceCollection AddXunitLogging(this IServiceCollection services, XunitOutputAccessor output)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new XunitLoggerProvider(output));
            logging.SetMinimumLevel(LogLevel.Information);
        });
        return services;
    }

    /// <summary>Makes <see cref="TestAuthHandler"/> the default scheme, replacing JWT bearer validation.</summary>
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services)
    {
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            options.DefaultScheme = TestAuthHandler.SchemeName;
        });
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        return services;
    }

    /// <summary>
    /// Removes the Azure Service Bus receiver hosted service(s) and swaps the transport for an in-memory
    /// no-op, so the outbox dispatcher drains without reaching a real broker.
    /// </summary>
    public static IServiceCollection RemoveAzureServiceBus(this IServiceCollection services)
    {
        var receivers = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType?.Name == AzureServiceBusReceiverTypeName)
            .ToList();
        foreach (var receiver in receivers)
            services.Remove(receiver);

        services.Replace(ServiceDescriptor.Singleton<IBusTransport, MockBusTransport>());
        return services;
    }
}
