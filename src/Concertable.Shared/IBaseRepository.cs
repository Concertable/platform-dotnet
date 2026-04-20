namespace Concertable.Application.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : class
{
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> AddAsync(TEntity entity);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task SaveChangesAsync();
}
