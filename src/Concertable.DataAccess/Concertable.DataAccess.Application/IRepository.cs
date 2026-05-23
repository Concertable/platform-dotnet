using Concertable.Kernel;

namespace Concertable.DataAccess.Application;

public interface IRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, IIdEntity
{
    Task<TEntity?> GetByIdAsync(int id);
    bool Exists(int id);
}
