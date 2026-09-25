using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Concertable.Messaging.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMessaging()
        {
            services.AddScoped<IBus, Bus>();
            services.AddScoped<IBusTransport, InMemoryBusTransport>();
            return services;
        }

        public IServiceCollection AddInMemoryTransport()
        {
            services.AddScoped<IBusTransport, InMemoryBusTransport>();
            return services;
        }

        public IServiceCollection AddDirectBusKeyed(string key = "direct")
        {
            services.AddKeyedScoped<IBus, Bus>(key);
            return services;
        }

        public IServiceCollection AddInbox(Action<DbContextOptionsBuilder> configureDb)
        {
            services.AddDbContext<InboxDbContext>(configureDb);
            return services;
        }

        public IServiceCollection AddOutbox(
            Action<DbContextOptionsBuilder> configureDb,
            Action<OutboxOptions>? configure = null,
            bool runDispatcher = true) =>
            services.AddOutbox((_, options) => configureDb(options), configure, runDispatcher);

        public IServiceCollection AddOutbox(
            Action<IServiceProvider, DbContextOptionsBuilder> configureDb,
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

        public IServiceCollection AddInProcessEventDispatch()
        {
            services.AddScoped<OutboxBus>();
            services.AddScoped<IBus>(sp => new LocalDispatchingBus(
                sp.GetRequiredService<OutboxBus>(),
                sp,
                sp.GetRequiredService<TimeProvider>()));
            return services;
        }

        /// <summary>
        /// Registers <see cref="CompositePausable"/>, which pauses every <see cref="IPausable"/> in the host at
        /// once. Resolve it by its concrete type: registered as <see cref="IPausable"/> it would enumerate itself.
        /// </summary>
        public IServiceCollection AddCompositePausable()
        {
            services.AddSingleton<CompositePausable>();
            return services;
        }
    }
}
