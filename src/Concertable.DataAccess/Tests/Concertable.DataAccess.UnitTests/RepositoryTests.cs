using System.Reflection;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.UnitTests;

public sealed class RepositoryTests
{
    [Fact]
    public void Repository_FacetFields_ComposesSharedImplementations()
    {
        var fields = typeof(Repository<TestEntity, TestDbContext, int>)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Select(field => field.FieldType)
            .ToArray();

        Assert.Contains(typeof(ReadRepository<TestEntity, int>), fields);
        Assert.Contains(typeof(WriteRepository<TestEntity, TestDbContext>), fields);
    }

    [Fact]
    public async Task Repository_ReadThenSave_PreservesOneTrackedUnitOfWork()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateContext(databaseName, root);
        var repository = new TestRepository(context);
        var entity = new TestEntity { Name = "Original" };
        await repository.InsertAsync(entity);

        var loaded = await repository.GetByIdAsync(entity.Id);
        loaded!.Name = "Updated";
        await repository.SaveChangesAsync();

        Assert.Same(entity, loaded);
        await using var verificationContext = CreateContext(databaseName, root, QueryTrackingBehavior.NoTracking);
        Assert.Equal("Updated", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task ReadRepository_DedicatedNoTrackingContext_IsolatedFromTrackedChanges()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        await using var trackedContext = CreateContext(databaseName, root);
        var trackedRepository = new TestRepository(trackedContext);
        var entity = new TestEntity { Name = "Persisted" };
        await trackedRepository.InsertAsync(entity);
        entity.Name = "Unsaved";
        await using var readContext = CreateContext(databaseName, root, QueryTrackingBehavior.NoTracking);
        var readRepository = new TestReadRepository(readContext);

        var isolated = await readRepository.GetByIdAsync(entity.Id);

        Assert.Equal("Persisted", isolated!.Name);
        Assert.NotSame(entity, isolated);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public void IReadDbContext_PublicSurface_ExposesQueryOnly()
    {
        var methods = typeof(IReadDbContext).GetMethods();

        Assert.Single(methods);
        Assert.Equal(nameof(IReadDbContext.Query), methods[0].Name);
    }

    private static TestDbContext CreateContext(
        string databaseName,
        InMemoryDatabaseRoot root,
        QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .UseQueryTrackingBehavior(trackingBehavior)
            .Options;
        return new TestDbContext(options);
    }

    private sealed class TestRepository : Repository<TestEntity, TestDbContext, int>
    {
        public TestRepository(TestDbContext context)
            : base(context) { }
    }

    private sealed class TestReadRepository : ReadRepository<TestEntity, int>
    {
        public TestReadRepository(IReadDbContext context)
            : base(context) { }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestEntity : IIdEntity
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
    }
}
