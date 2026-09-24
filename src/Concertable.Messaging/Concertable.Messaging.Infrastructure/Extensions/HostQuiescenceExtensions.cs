using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.Infrastructure.Extensions;

public static class HostQuiescenceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the <see cref="IHostQuiescence"/> aggregate over every <see cref="IIngressQuiescer"/> the
        /// host has registered — the ASB receiver, the outbox dispatcher, and any host-owned participant such
        /// as the HTTP request pipeline. Call it once per host that must reach a quiescent state.
        /// </summary>
        public IServiceCollection AddHostQuiescence()
        {
            services.AddSingleton<IHostQuiescence, HostQuiescence>();
            return services;
        }
    }
}
