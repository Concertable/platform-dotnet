using Concertable.Kernel;

namespace Concertable.DataAccess.Application;

public interface IRepository<TEntity, TKey> : IBaseRepository<TEntity>, IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>;

public interface IRepository<TEntity> : IRepository<TEntity, int>
    where TEntity : class, IIdEntity;
