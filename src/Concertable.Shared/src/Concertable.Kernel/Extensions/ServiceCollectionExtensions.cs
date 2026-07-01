using Concertable.Contracts;
using Concertable.Kernel.Auth;
using Concertable.Kernel.Background;
using Concertable.Kernel.Events;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services;
using Concertable.Kernel.Services.Geometry;
using Concertable.Kernel.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using Refit;

namespace Concertable.Kernel.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        services.AddSingleton<IBackgroundTaskRunner, BackgroundTaskRunner>();

        services.Configure<UrlSettings>(configuration.GetSection("Urls"));
        services.AddScoped<IUriService, UriService>();

        return services;
    }

    public static IServiceCollection AddGeometry(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IGeometryProvider, GeographicGeometryProvider>(GeometryProviderType.Geographic, (_, _) =>
            new GeographicGeometryProvider(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326)));
        services.AddKeyedSingleton<IGeometryProvider, MetricGeometryProvider>(GeometryProviderType.Metric, (_, _) =>
            new MetricGeometryProvider(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857)));
        services.AddSingleton<IGeometryCalculator, GeometryCalculator>();
        return services;
    }

    public static IServiceCollection AddQueueHostedService(this IServiceCollection services)
    {
        services.AddHostedService<QueueHostedService>();
        return services;
    }

    public static IServiceCollection AddClientCredentials(
        this IServiceCollection services,
        Action<TokenServiceOptions> configure)
    {
        services.Configure(configure);

        // The authority is the Refit base address — resolve it now from the same delegate the options bind from.
        var options = new TokenServiceOptions();
        configure(options);

        services.AddRefitClient<ITokenApi>()
            .ConfigureHttpClient(client =>
            {
                // Empty authority defers the failure to the first token request (as before), not to startup.
                if (!string.IsNullOrWhiteSpace(options.Authority))
                    client.BaseAddress = new Uri(options.Authority.TrimEnd('/'));
            });

        services.AddSingleton<ITokenService, ClientCredentialsTokenService>();
        return services;
    }

    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        return services;
    }

}
