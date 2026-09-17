using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException ex)
    {
        public bool IsDuplicateKey() => ex.InnerException is DbException dbEx && dbEx.IsDuplicateKey();

        public void DiscardFailedChanges()
        {
            foreach (var entry in ex.Entries)
                entry.State = EntityState.Detached;
        }
    }
}
