using Concertable.Seed.Shared.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Concertable.Seed.Shared.Extensions;

public static class SeedingDbContextOptionsExtensions
{
    extension(DbContextOptionsBuilder builder)
    {
        public DbContextOptionsBuilder UseSeedingSupport(IServiceProvider provider)
            => builder.AddInterceptors(provider.GetRequiredService<SeedingIdentityInterceptor>());

        public DbContextOptionsBuilder UseSeedChain(IServiceProvider provider)
            => builder
                .UseSeeding((context, _) =>
                    SeedChain.SeedAsync(provider.GetSeeders(context), provider.CreateSeedChainLogger())
                        .GetAwaiter().GetResult())
                .UseAsyncSeeding((context, _, ct) =>
                    SeedChain.SeedAsync(provider.GetSeeders(context), provider.CreateSeedChainLogger(), ct));
    }

    extension(IServiceProvider provider)
    {
        private IEnumerable<ISeeder> GetSeeders(DbContext context)
            => provider.GetKeyedServices<ISeeder>(context.GetType());

        private ILogger CreateSeedChainLogger()
            => provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedChain));
    }
}
