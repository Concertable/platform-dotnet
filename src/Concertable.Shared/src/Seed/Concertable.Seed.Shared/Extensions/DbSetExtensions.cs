using Microsoft.EntityFrameworkCore;

namespace Concertable.Seed.Shared.Extensions;

public static class DbSetExtensions
{
    extension<TEntity>(DbSet<TEntity> set)
        where TEntity : class
    {
        public async Task SeedIfEmptyAsync(Func<Task> seedAction)
        {
            if (!await set.AnyAsync())
                await seedAction();
        }
    }
}
