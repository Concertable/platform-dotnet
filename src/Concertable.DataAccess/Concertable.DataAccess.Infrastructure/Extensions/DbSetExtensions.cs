using System.Linq.Expressions;
using FlexLabs.EntityFrameworkCore.Upsert;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class DbSetExtensions
{
    public static async Task<TEntity> GetOrCreateAsync<TEntity>(
        this DbSet<TEntity> set,
        TEntity candidate,
        Expression<Func<TEntity, object>> matchOn,
        Expression<Func<TEntity, bool>> find,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        try
        {
            await set.Upsert(candidate).On(matchOn).NoUpdate().RunAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.IsDuplicateKey()) { }

        return await set.FirstAsync(find, cancellationToken);
    }
}
