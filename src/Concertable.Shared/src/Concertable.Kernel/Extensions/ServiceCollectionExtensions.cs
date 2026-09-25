using Concertable.Contracts;
using Concertable.Kernel.Auth;
using Concertable.Kernel.Background;
using Concertable.Kernel.DependencyInjection;
using Concertable.Kernel.Events;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Services.Geometry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using Refit;

namespace Concertable.Kernel.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharedInfrastructure(IConfiguration configuration)
        {
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<IBackgroundTaskRunner, BackgroundTaskRunner>();
            services.TryAddSingleton(typeof(IScoped<>), typeof(Scoped<>));

            return services;
        }

        public IServiceCollection AddGeometry()
        {
            services.AddKeyedSingleton<IGeometryProvider, GeographicGeometryProvider>(GeometryProviderType.Geographic, (_, _) =>
                new GeographicGeometryProvider(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326)));
            services.AddKeyedSingleton<IGeometryProvider, MetricGeometryProvider>(GeometryProviderType.Metric, (_, _) =>
                new MetricGeometryProvider(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857)));
            services.AddSingleton<IGeometryCalculator, GeometryCalculator>();
            return services;
        }

        public IServiceCollection AddQueueHostedService()
        {
            services.AddHostedService<QueueHostedService>();
            return services;
        }

        public IServiceCollection AddClientCredentials(Action<TokenServiceOptions> configure)
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

        public IServiceCollection AddCurrentUser()
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUserAccessor>();
            return services;
        }
    }
}
