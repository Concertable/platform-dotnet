using Concertable.Kernel;

namespace Concertable.DataAccess.Application;

public interface IReadRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    bool Exists(TKey id);
}

public interface IReadRepository<TEntity> : IReadRepository<TEntity, int>
    where TEntity : class, IIdEntity;
