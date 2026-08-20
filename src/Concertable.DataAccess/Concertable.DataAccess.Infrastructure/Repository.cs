using Concertable.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public abstract class WriteRepository<TEntity> : IWriteRepository<TEntity>
    where TEntity : class
{
    protected IWriteDbContext Context { get; }

    protected WriteRepository(IWriteDbContext context)
    {
        this.Context = context;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await Context.AddAsync(entity, ct);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default)
    {
        await Context.AddRangeAsync(entities, ct);
        return entities;
    }

    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default)
    {
        await Context.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public void Update(TEntity entity) => Context.Update(entity);

    public void Remove(TEntity entity) => Context.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await Context.SaveChangesAsync(ct);
}

public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected IDbContext Context { get; }

    protected Repository(IDbContext context)
    {
        this.Context = context;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        await Context.Query<TEntity>().ToListAsync(ct);

    public virtual Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        Context.Query<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);

    public bool Exists(TKey id) => Context.Query<TEntity>().Any(e => e.Id!.Equals(id));

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await Context.AddAsync(entity, ct);
        return entity;
    }

    public async Task<IEnumerable<TEntity>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken ct = default)
    {
        await Context.AddRangeAsync(entities, ct);
        return entities;
    }

    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default)
    {
        await Context.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public void Update(TEntity entity) => Context.Update(entity);

    public void Remove(TEntity entity) => Context.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await Context.SaveChangesAsync(ct);
}

public abstract class ReadRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected IReadDbContext Context { get; }

    public ReadRepository(IReadDbContext context)
    {
        this.Context = context;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        await Context.Query<TEntity>().ToListAsync(ct);

    public virtual Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        Context.Query<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);

    public bool Exists(TKey id) =>
        Context.Query<TEntity>().Any(e => e.Id!.Equals(id));
}
