using Concertable.Kernel;

namespace Concertable.DataAccess.Application;

public interface IReadRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    bool Exists(TKey id);
}

public interface IReadRepository<TEntity> : IReadRepository<TEntity, int>
    where TEntity : class, IIdEntity;
