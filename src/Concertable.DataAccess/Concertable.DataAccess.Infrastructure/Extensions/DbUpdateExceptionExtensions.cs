using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbUpdateExceptionExtensions
{
    public static bool IsDuplicateKey(this DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx &&
            (sqlEx.Number == 2601 || sqlEx.Number == 2627);
    }

    public static void DiscardFailedChanges(this DbUpdateException ex)
    {
        foreach (var entry in ex.Entries)
            entry.State = EntityState.Detached;
    }
}
