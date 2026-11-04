using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Locking;

public static class RowLockExtensions
{
    internal const string TagPrefix = "concertable:row-lock:";

    extension<TEntity>(DbSet<TEntity> set)
        where TEntity : class
    {
        public async Task<bool> LockRowsAsync(
            Expression<Func<TEntity, bool>> predicate,
            RowLock mode = RowLock.NoKeyUpdate,
            CancellationToken ct = default)
        {
            var keyNames = set.EntityType.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray();
            var query = set.TagWith(TagPrefix + mode).Where(predicate)
                .OrderBy(entity => EF.Property<object>(entity, keyNames[0]));
            foreach (var keyName in keyNames.Skip(1))
                query = query.ThenBy(entity => EF.Property<object>(entity, keyName));
            return (await query.Select(_ => 1).ToListAsync(ct)).Count > 0;
        }
    }
}
