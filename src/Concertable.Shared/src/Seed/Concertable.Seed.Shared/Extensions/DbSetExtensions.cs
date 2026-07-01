using Microsoft.EntityFrameworkCore;

namespace Concertable.Seed.Shared.Extensions;

public static class DbSetExtensions
{
    public static async Task SeedIfEmptyAsync<TEntity>(
        this DbSet<TEntity> set,
        Func<Task> seedAction)
        where TEntity : class
    {
        if (!await set.AnyAsync())
            await seedAction();
    }
}
