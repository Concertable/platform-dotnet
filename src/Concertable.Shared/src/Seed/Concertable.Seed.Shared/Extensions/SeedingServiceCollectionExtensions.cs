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
            services.AddKeyedScoped<ISeeder, TSeeder>(typeof(TContext));
            return services;
        }
    }
}
