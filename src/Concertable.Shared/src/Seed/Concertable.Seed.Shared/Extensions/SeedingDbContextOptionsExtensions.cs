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
                    SeedChain.SeedAsync(SeedersFor(provider, context), LoggerFor(provider))
                        .GetAwaiter().GetResult())
                .UseAsyncSeeding((context, _, ct) =>
                    SeedChain.SeedAsync(SeedersFor(provider, context), LoggerFor(provider), ct));
    }

    private static IEnumerable<ISeeder> SeedersFor(IServiceProvider provider, DbContext context)
        => provider.GetKeyedServices<ISeeder>(context.GetType());

    private static ILogger LoggerFor(IServiceProvider provider)
        => provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedChain));
}
