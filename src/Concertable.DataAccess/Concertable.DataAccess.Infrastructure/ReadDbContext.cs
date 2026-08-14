using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public abstract class ReadDbContext : DbContext, IReadDbContext
{
    private readonly IEntityTypeConfigurationProvider provider;
    private readonly string defaultSchema;

    protected ReadDbContext(
        DbContextOptions options,
        IEntityTypeConfigurationProvider provider,
        string defaultSchema)
        : base(options)
    {
        this.provider = provider;
        this.defaultSchema = defaultSchema;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => Set<TEntity>();

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(defaultSchema);
        provider.Configure(modelBuilder);
    }

    public sealed override int SaveChanges() =>
        throw new InvalidOperationException("Read contexts are read-only.");

    public sealed override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException("Read contexts are read-only.");

    public sealed override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Read contexts are read-only.");

    public sealed override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Read contexts are read-only.");
}
