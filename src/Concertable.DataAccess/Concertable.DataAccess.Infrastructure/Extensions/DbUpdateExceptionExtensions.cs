using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbUpdateExceptionExtensions
{
    public static bool IsDuplicateKey(this DbUpdateException ex) =>
        ex.InnerException is DbException dbEx && dbEx.IsDuplicateKey();

    public static void DiscardFailedChanges(this DbUpdateException ex)
    {
        foreach (var entry in ex.Entries)
            entry.State = EntityState.Detached;
    }
}
