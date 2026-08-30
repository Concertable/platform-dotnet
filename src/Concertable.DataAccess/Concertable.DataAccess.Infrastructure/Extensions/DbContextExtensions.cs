using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbContextExtensions
{
    extension(DbContext context)
    {
        /// <summary>Saves pending changes. An EF update failure returns <c>false</c> and clears the complete
        /// change tracker; every other failure propagates.</summary>
        public async Task<bool> TrySaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await context.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                context.ChangeTracker.Clear();
                return false;
            }
        }
    }
}
