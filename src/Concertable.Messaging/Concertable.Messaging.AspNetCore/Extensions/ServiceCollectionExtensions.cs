using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers HTTP requests as an <see cref="IPausable"/>, so pausing the host holds new requests and
        /// waits for in-flight ones. Pair it with <c>UsePausableRequests()</c>, which inserts
        /// <see cref="PausableRequestsMiddleware"/>.
        /// </summary>
        public IServiceCollection AddPausableRequests(Action<PausableRequestsOptions>? configure = null)
        {
            services.AddHttpContextAccessor();
            if (configure is not null)
                services.Configure(configure);
            else
                services.AddOptions<PausableRequestsOptions>();
            services.AddSingleton<PausableRequests>();
            services.AddSingleton<IPausable>(sp => sp.GetRequiredService<PausableRequests>());
            services.AddSingleton<PausableRequestsMiddleware>();
            return services;
        }
    }
}
