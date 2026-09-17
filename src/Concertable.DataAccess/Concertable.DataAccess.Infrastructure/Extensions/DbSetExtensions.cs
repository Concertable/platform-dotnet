using System.Data.Common;
using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbSetExtensions
{
    private const string ContestedInsertSavepoint = "get_or_create";

    extension<TEntity>(DbSet<TEntity> set)
        where TEntity : class
    {
        public async Task<TEntity> GetOrCreateAsync(
            TEntity candidate,
            Expression<Func<TEntity, object>> matchOn,
            Expression<Func<TEntity, bool>> find,
            CancellationToken cancellationToken = default)
        {
            var transaction = set.GetService<ICurrentDbContext>().Context.Database.CurrentTransaction;
            if (transaction is not null)
                await transaction.CreateSavepointAsync(ContestedInsertSavepoint, cancellationToken);

            try
            {
                await set.Upsert(candidate).On(matchOn).NoUpdate().RunAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                // PostgreSQL aborts the caller's transaction on a failed statement; without this rewind the read below fails with 25P02.
                if (transaction is not null)
                    await transaction.RollbackToSavepointAsync(ContestedInsertSavepoint, cancellationToken);
                if (!ex.IsDuplicateKey())
                    throw;
            }
            finally
            {
                if (transaction is not null)
                    await transaction.ReleaseSavepointAsync(ContestedInsertSavepoint, cancellationToken);
            }

            return await set.FirstAsync(find, cancellationToken);
        }
    }
}
