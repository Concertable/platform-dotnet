using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Locking;

public static class RowLockExtensions
{
    internal const string Tag = "concertable:row-lock";

    extension<TEntity>(IQueryable<TEntity> query)
    {
        public IQueryable<TEntity> WithRowLock() => query.TagWith(Tag);
    }
}
