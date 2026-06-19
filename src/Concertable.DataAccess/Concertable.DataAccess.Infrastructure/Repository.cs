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

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<TEntity>().ToListAsync(ct);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await context.Set<TEntity>().AddAsync(entity, ct);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await context.Set<TEntity>().AddRangeAsync(entities, ct);
        return entities;
    }

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}

public abstract class ReadRepository<TEntity, TContext, TKey>(TContext context)
    : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TContext : DbContextBase
{
    protected readonly TContext context = context;

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<TEntity>().ToListAsync(ct);

    public virtual Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);

    public bool Exists(TKey id) =>
        context.Set<TEntity>().Any(e => e.Id!.Equals(id));
}

public abstract class Repository<TEntity, TContext, TKey>(TContext context)
    : BaseRepository<TEntity, TContext>(context), IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TContext : DbContextBase
{
    public virtual Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);

    public bool Exists(TKey id) =>
        context.Set<TEntity>().Any(e => e.Id!.Equals(id));
}
