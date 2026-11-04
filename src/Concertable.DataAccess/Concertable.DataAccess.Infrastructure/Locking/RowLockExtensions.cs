using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Locking;

public static class RowLockExtensions
{
    internal const string TagPrefix = "concertable:row-lock:";

    extension<TEntity>(IQueryable<TEntity> query)
    {
        public async Task<bool> LockRowsAsync(
            Expression<Func<TEntity, bool>> predicate,
            RowLock mode = RowLock.NoKeyUpdate,
            CancellationToken ct = default) =>
            (await query.TagWith(TagPrefix + mode).Where(predicate).Select(_ => 1).ToListAsync(ct)).Count > 0;
    }
}
