using Concertable.Kernel;

namespace Concertable.DataAccess.Application;

public interface IRepository<TEntity, TKey> : IBaseRepository<TEntity>, IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    new Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
}

public interface IRepository<TEntity> : IRepository<TEntity, int>
    where TEntity : class, IIdEntity;
