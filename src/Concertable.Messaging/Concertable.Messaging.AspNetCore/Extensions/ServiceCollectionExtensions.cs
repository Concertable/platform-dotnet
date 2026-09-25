using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="GateMiddleware"/> as an <see cref="IPausable"/>, so pausing the host holds new
        /// requests and waits for in-flight ones. Pair it with <c>UseGate()</c>.
        /// </summary>
        public IServiceCollection AddGate(Action<GateOptions>? configure = null)
        {
            services.AddHttpContextAccessor();
            if (configure is not null)
                services.Configure(configure);
            else
                services.AddOptions<GateOptions>();
            services.AddSingleton<GateMiddleware>();
            services.AddSingleton<IPausable>(sp => sp.GetRequiredService<GateMiddleware>());
            return services;
        }
    }
}
