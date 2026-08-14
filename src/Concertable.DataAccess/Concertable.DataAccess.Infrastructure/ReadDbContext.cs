using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure;

public abstract class ReadDbContext : DbContext, IReadDbContext
{
    protected ReadDbContext(DbContextOptions options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => Set<TEntity>();

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
