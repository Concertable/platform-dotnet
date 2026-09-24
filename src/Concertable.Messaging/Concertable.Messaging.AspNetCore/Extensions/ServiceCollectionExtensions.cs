using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the HTTP request pipeline as an <see cref="IIngressQuiescer"/> so
        /// <see cref="IHostQuiescence"/> drains in-flight requests. Pair it with
        /// <c>UseHostQuiescence()</c>, which inserts <see cref="RequestQuiescenceMiddleware"/>.
        /// </summary>
        public IServiceCollection AddHttpRequestQuiescence(Action<HttpQuiescenceOptions>? configure = null)
        {
            services.AddHttpContextAccessor();
            if (configure is not null)
                services.Configure(configure);
            else
                services.AddOptions<HttpQuiescenceOptions>();
            services.AddSingleton<HttpIngressQuiescer>();
            services.AddSingleton<IIngressQuiescer>(sp => sp.GetRequiredService<HttpIngressQuiescer>());
            services.AddSingleton<RequestQuiescenceMiddleware>();
            return services;
        }
    }
}
