using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbContextExtensions
{
    extension(DbContext context)
    {
        public async Task<bool> TrySaveChangesAsync(
            Func<DbUpdateException, bool> isExpected,
            CancellationToken ct = default)
        {
            try
            {
                await context.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException exception) when (isExpected(exception))
            {
                context.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
