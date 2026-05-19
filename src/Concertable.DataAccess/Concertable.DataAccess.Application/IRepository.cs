using Concertable.Shared;

namespace Concertable.DataAccess;

public interface IRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, IIdEntity
{
    Task<TEntity?> GetByIdAsync(int id);
    bool Exists(int id);
}
