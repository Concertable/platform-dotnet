using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Concertable.Messaging.Infrastructure.Extensions;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb,
        Action<OutboxOptions>? configure = null,
        bool runDispatcher = true)
    {
        if (configure is not null) services.Configure(configure);
        else services.AddOptions<OutboxOptions>();

        services.AddDbContext<OutboxDbContext>(configureDb);
        services.AddScoped<IDbContextAccessor, DbContextAccessor>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IOutboxReader, OutboxReader>();
        services.AddScoped<IBus, OutboxBus>();
        if (runDispatcher)
        {
            services.AddScoped<EventDispatcher>();
            services.AddScoped<CommandDispatcher>();
            services.AddScoped<IMessageDispatchResolver, MessageDispatchResolver>();
            services.AddHostedService<OutboxDispatcher>();
        }
        services.TryAddSingleton<MessageSerializer>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    public static IServiceCollection AddInProcessEventDispatch(this IServiceCollection services)
    {
        services.AddScoped<OutboxBus>();
        services.AddScoped<IBus>(sp => new LocalDispatchingBus(
            sp.GetRequiredService<OutboxBus>(),
            sp,
            sp.GetRequiredService<TimeProvider>()));
        return services;
    }
}
