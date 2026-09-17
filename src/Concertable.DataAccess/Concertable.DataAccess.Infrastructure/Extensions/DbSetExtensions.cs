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

    public static async Task<TEntity> GetOrCreateAsync<TEntity>(
        this DbSet<TEntity> set,
        TEntity candidate,
        Expression<Func<TEntity, object>> matchOn,
        Expression<Func<TEntity, bool>> find,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        // PostgreSQL aborts the whole transaction on the failed insert, so without a savepoint to roll
        // back to, the read below fails with 25P02 instead of returning the row that won.
        var transaction = set.GetService<ICurrentDbContext>().Context.Database.CurrentTransaction;
        if (transaction is not null)
            await transaction.CreateSavepointAsync(ContestedInsertSavepoint, cancellationToken);

        try
        {
            await set.Upsert(candidate).On(matchOn).NoUpdate().RunAsync(cancellationToken);

            if (transaction is not null)
                await transaction.ReleaseSavepointAsync(ContestedInsertSavepoint, cancellationToken);
        }
        catch (DbException ex) when (ex.IsDuplicateKey())
        {
            if (transaction is not null)
                await transaction.RollbackToSavepointAsync(ContestedInsertSavepoint, cancellationToken);
        }

        return await set.FirstAsync(find, cancellationToken);
    }
}
