namespace Concertable.DataAccess.Application;

public interface IWriteDbContext
{
    Task AddAsync<TEntity>(TEntity entity, CancellationToken ct = default) where TEntity : class;
    Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct = default) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
