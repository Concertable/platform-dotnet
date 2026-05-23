using Concertable.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public abstract class BaseRepository<TEntity, TContext>(TContext context)
    : IBaseRepository<TEntity>
    where TEntity : class
    where TContext : DbContextBase
{
    protected readonly TContext context = context;

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        await context.Set<TEntity>().AddAsync(entity);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await context.Set<TEntity>().AddRangeAsync(entities);
        return entities;
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync() =>
        await context.Set<TEntity>().ToListAsync();

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}

public abstract class GuidRepository<TEntity, TContext>(TContext context)
    : BaseRepository<TEntity, TContext>(context), IGuidRepository<TEntity>
    where TEntity : class, IGuidEntity
    where TContext : DbContextBase
{
    public Task<TEntity?> GetByIdAsync(Guid id) =>
        context.Set<TEntity>().FindAsync(id).AsTask();

    public bool Exists(Guid id) =>
        context.Set<TEntity>().Any(e => e.Id == id);
}

public abstract class Repository<TEntity, TContext>(TContext context)
    : BaseRepository<TEntity, TContext>(context), IIdRepository<TEntity>
    where TEntity : class, IIdEntity
    where TContext : DbContextBase
{
    public Task<TEntity?> GetByIdAsync(int id) =>
        context.Set<TEntity>().FindAsync(id).AsTask();

    public bool Exists(int id) =>
        context.Set<TEntity>().Any(e => e.Id == id);
}
