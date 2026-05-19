using Concertable.Shared;

namespace Concertable.DataAccess;

public interface IGuidRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, IGuidEntity
{
    Task<TEntity?> GetByIdAsync(Guid id);
    bool Exists(Guid id);
}
