using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Concertable.Seed.Shared;

public static class SeedChain
{
    public static async Task MigrateAsync(IEnumerable<ISeeder> seeders, ILogger logger, CancellationToken ct = default)
    {
        foreach (var seeder in Ordered(seeders))
        {
            logger.MigratingSeeder(seeder.GetType().Name);
            await seeder.MigrateAsync(ct);
        }
    }

    public static async Task SeedAsync(IEnumerable<ISeeder> seeders, ILogger logger, CancellationToken ct = default)
    {
        var ordered = Ordered(seeders);
        if (ordered.Count == 0)
            return;

        logger.BeginSeedChain(ordered.Count);
        var total = Stopwatch.StartNew();

        foreach (var seeder in ordered)
        {
            var name = seeder.GetType().Name;
            logger.SeedingSeeder(name, seeder.Order);
            var sw = Stopwatch.StartNew();
            try
            {
                await seeder.SeedAsync(ct);
                sw.Stop();
                logger.SeederCompleted(name, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                logger.SeederFailed(name, sw.ElapsedMilliseconds, ex);
                throw;
            }
        }

        total.Stop();
        logger.SeedChainComplete(total.ElapsedMilliseconds);
    }

    private static List<ISeeder> Ordered(IEnumerable<ISeeder> seeders) =>
        [.. seeders.OrderBy(seeder => seeder.Order)];
}
