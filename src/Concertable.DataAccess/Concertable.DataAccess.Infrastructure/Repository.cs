using Concertable.DataAccess.Application;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public abstract class WriteRepository<TEntity, TContext> : IWriteRepository<TEntity>
    where TEntity : class
    where TContext : DbContextBase
{
    protected readonly TContext context;

    public WriteRepository(TContext context)
    {
        this.context = context;
    }

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

    public async Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default)
    {
        await context.Set<TEntity>().AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
        return entity;
    }

    public void Update(TEntity entity) => context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => context.Set<TEntity>().Remove(entity);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}

public abstract class ReadRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    protected readonly IReadDbContext context;

    public ReadRepository(IReadDbContext context)
    {
        this.context = context;
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        await context.Query<TEntity>().ToListAsync(ct);

    public virtual Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        context.Query<TEntity>().FirstOrDefaultAsync(e => e.Id!.Equals(id), ct);

    public bool Exists(TKey id) =>
        context.Query<TEntity>().Any(e => e.Id!.Equals(id));
}

public abstract class Repository<TEntity, TContext, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TContext : DbContextBase
{
    private readonly ReadRepository<TEntity, TKey> readRepository;
    private readonly WriteRepository<TEntity, TContext> writeRepository;
    protected readonly TContext context;

    protected Repository(TContext context)
    {
        this.context = context;
        this.readRepository = new ReadFacet(context);
        this.writeRepository = new WriteFacet(context);
    }

    public virtual Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        readRepository.GetByIdAsync(id, ct);

    public virtual Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default) =>
        readRepository.GetAllAsync(ct);

    public bool Exists(TKey id) => readRepository.Exists(id);

    public Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default) =>
        writeRepository.AddAsync(entity, ct);

    public Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) =>
        writeRepository.AddRangeAsync(entities, ct);

    public Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default) =>
        writeRepository.InsertAsync(entity, ct);

    public void Update(TEntity entity) => writeRepository.Update(entity);

    public void Remove(TEntity entity) => writeRepository.Remove(entity);

    public Task SaveChangesAsync(CancellationToken ct = default) => writeRepository.SaveChangesAsync(ct);

    private sealed class ReadFacet : ReadRepository<TEntity, TKey>
    {
        public ReadFacet(IReadDbContext context)
            : base(context) { }
    }

    private sealed class WriteFacet : WriteRepository<TEntity, TContext>
    {
        public WriteFacet(TContext context)
            : base(context) { }
    }
}
