using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel;
using Concertable.Testing.Unit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.DataAccess.UnitTests;

public sealed class WriteRepositoryExtensionsTests
{
    [Fact]
    public async Task TryInsertAsync_SaveFailsOnAPostgresUniqueViolation_ReturnsFalseAndDetachesTheEntity()
    {
        await using var context = CreateContext(PostgresError(PostgresErrorCodes.UniqueViolation));
        var repository = new TestWriteRepository(context);
        var entity = new TestEntity { Name = "Contested" };

        var inserted = await repository.TryInsertAsync(entity);

        Assert.False(inserted);
        Assert.Equal(EntityState.Detached, context.Entry(entity).State);
    }

    [Fact]
    public async Task TryInsertAsync_SaveFailsOnAPostgresForeignKeyViolation_Throws()
    {
        await using var context = CreateContext(PostgresError(PostgresErrorCodes.ForeignKeyViolation));
        var repository = new TestWriteRepository(context);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.TryInsertAsync(new TestEntity { Name = "Orphaned" }));
    }

    private static ConflictingDbContext CreateContext(Exception inner)
    {
        var (root, databaseName) = InMemoryDatabaseFactory.Create();
        return root.CreateContext<ConflictingDbContext>(
            databaseName,
            options => new ConflictingDbContext(options, inner));
    }

    private static PostgresException PostgresError(string sqlState) =>
        new("violation", "ERROR", "ERROR", sqlState);

    private sealed class ConflictingDbContext(DbContextOptions<ConflictingDbContext> options, Exception inner)
        : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("save failed", inner, ChangeTracker.Entries().ToList());
    }

    private sealed class TestWriteRepository(IWriteDbContext context) : WriteRepository<TestEntity>(context);

    private sealed class TestEntity : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
