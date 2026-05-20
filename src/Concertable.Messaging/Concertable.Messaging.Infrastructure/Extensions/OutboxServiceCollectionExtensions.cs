using Concertable.Messaging.Application;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Concertable.Messaging.Infrastructure.Extensions;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox<TContext>(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null)
        where TContext : DbContext
    {
        if (configure is not null) services.Configure(configure);
        else services.AddOptions<OutboxOptions>();

        services.AddScoped<IOutboxStore, OutboxStore<TContext>>();
        services.AddScoped<IBus, OutboxBus>();
        services.AddHostedService<OutboxDispatcher<TContext>>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
