using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Locking;

public static class RowLockExtensions
{
    internal const string TagPrefix = "concertable:row-lock:";

    extension<TEntity>(IQueryable<TEntity> query)
    {
        public IQueryable<TEntity> WithRowLock(RowLock mode = RowLock.NoKeyUpdate) =>
            query.TagWith(TagPrefix + mode);
    }
}
