using Concertable.Seed.Shared.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Seed.Shared.Extensions;

public static class SeedingServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeedingInfrastructure()
        {
            services.AddSingleton<SeedingScope>();
            services.AddSingleton<SeedingIdentityInterceptor>();
            return services;
        }

        public IServiceCollection AddSeeder<TContext, TSeeder>()
            where TContext : DbContext
            where TSeeder : class, ISeeder
        {
            if (typeof(IStandInSeeder).IsAssignableFrom(typeof(TSeeder)))
                throw new InvalidOperationException(
                    $"{typeof(TSeeder).Name} is an {nameof(IStandInSeeder)}, so it writes rows a producer " +
                    $"owns and must never run in dev or E2E. Register it with AddStandInSeeder from the " +
                    $"integration composition instead.");

            services.AddKeyedScoped<ISeeder, TSeeder>(typeof(TContext));
            return services;
        }

        public IServiceCollection AddStandInSeeder<TContext, TSeeder>()
            where TContext : DbContext
            where TSeeder : class, IStandInSeeder
        {
            services.AddKeyedScoped<ISeeder, TSeeder>(typeof(TContext));
            return services;
        }
    }
}
