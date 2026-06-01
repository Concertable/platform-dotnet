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

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() =>
        await context.Set<TEntity>().ToListAsync();

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

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}

public abstract class ReadRepository<TEntity, TContext, TKey>(TContext context)
    : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TContext : DbContextBase
{
    protected readonly TContext context = context;

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync() =>
        await context.Set<TEntity>().ToListAsync();

    public virtual Task<TEntity?> GetByIdAsync(TKey id) =>
        context.Set<TEntity>().FindAsync(id).AsTask();

    public bool Exists(TKey id) =>
        context.Set<TEntity>().Any(e => e.Id!.Equals(id));
}

public abstract class Repository<TEntity, TContext, TKey>(TContext context)
    : BaseRepository<TEntity, TContext>(context), IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TContext : DbContextBase
{
    public virtual Task<TEntity?> GetByIdAsync(TKey id) =>
        context.Set<TEntity>().FindAsync(id).AsTask();

    public bool Exists(TKey id) =>
        context.Set<TEntity>().Any(e => e.Id!.Equals(id));
}
