using System.Reflection;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.UnitTests;

public sealed class RepositoryTests
{
    [Fact]
    public void Repository_ContextField_UsesCombinedCapabilityOnly()
    {
        var repositoryType = typeof(Repository<TestEntity, int>);
        var fields = repositoryType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Equal(typeof(object), repositoryType.BaseType);
        Assert.Single(fields);
        Assert.Equal(typeof(IDbContext), fields[0].FieldType);
    }

    [Fact]
    public void WriteRepository_ContextField_UsesWriteCapabilityOnly()
    {
        var repositoryType = typeof(WriteRepository<TestEntity>);
        var fields = repositoryType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Equal(typeof(object), repositoryType.BaseType);
        Assert.Single(fields);
        Assert.Equal(typeof(IWriteDbContext), fields[0].FieldType);
    }

    [Fact]
    public void ReadRepository_ContextField_UsesReadCapabilityOnly()
    {
        var repositoryType = typeof(ReadRepository<TestEntity, int>);
        var fields = repositoryType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.Equal(typeof(object), repositoryType.BaseType);
        Assert.Single(fields);
        Assert.Equal(typeof(IReadDbContext), fields[0].FieldType);
    }

    [Fact]
    public async Task WriteRepository_AddThenSave_PersistsThroughWriteCapability()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateContext(databaseName, root);
        var repository = new TestWriteRepository(context);
        var entity = new TestEntity { Name = "Persisted" };

        Assert.Same(entity, await repository.AddAsync(entity));
        await repository.SaveChangesAsync();

        await using var verificationContext = CreateReadContext(databaseName, root);
        Assert.Equal("Persisted", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task Repository_ReadThenSave_PreservesOneTrackedUnitOfWork()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        await using var context = CreateContext(databaseName, root);
        var repository = new TestCapabilityRepository(context);
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
        var trackedRepository = new TestCapabilityRepository(trackedContext);
        var entity = new TestEntity { Name = "Persisted" };
        await trackedRepository.InsertAsync(entity);
        entity.Name = "Unsaved";
        await using var readContext = CreateReadContext(databaseName, root);
        var readRepository = new TestReadRepository(readContext);

        var isolated = await readRepository.GetByIdAsync(entity.Id);

        Assert.Equal("Persisted", isolated!.Name);
        Assert.NotSame(entity, isolated);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ReadDbContext_SaveOverloads_RejectWrites()
    {
        var root = new InMemoryDatabaseRoot();
        using var context = CreateReadContext(Guid.NewGuid().ToString(), root);

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges(acceptAllChangesOnSuccess: false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync(acceptAllChangesOnSuccess: false));
    }

    [Fact]
    public void ReadDbContext_Model_ExcludesMessagingEntities()
    {
        var root = new InMemoryDatabaseRoot();
        using var context = CreateReadContext(Guid.NewGuid().ToString(), root);

        Assert.Null(context.Model.FindEntityType(typeof(InboxMessageEntity)));
        Assert.Null(context.Model.FindEntityType(typeof(OutboxMessageEntity)));
    }

    [Fact]
    public void IReadDbContext_PublicSurface_ExposesQueryOnly()
    {
        var methods = typeof(IReadDbContext).GetMethods();

        Assert.Single(methods);
        Assert.Equal(nameof(IReadDbContext.Query), methods[0].Name);
    }

    [Fact]
    public void IWriteDbContext_PublicSurface_ExposesMutationOnly()
    {
        var methodNames = typeof(IWriteDbContext)
            .GetMethods()
            .Select(method => method.Name)
            .Order()
            .ToArray();

        Assert.Equal(
            [
                nameof(IWriteDbContext.AddAsync),
                nameof(IWriteDbContext.AddRangeAsync),
                nameof(IWriteDbContext.Remove),
                nameof(IWriteDbContext.SaveChangesAsync),
                nameof(IWriteDbContext.Update)
            ],
            methodNames);
    }

    [Fact]
    public void IDbContext_PublicSurface_ComposesReadAndWriteCapabilities()
    {
        var interfaces = typeof(IDbContext).GetInterfaces();

        Assert.Equal(2, interfaces.Length);
        Assert.Contains(typeof(IReadDbContext), interfaces);
        Assert.Contains(typeof(IWriteDbContext), interfaces);
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

    private static TestReadDbContext CreateReadContext(string databaseName, InMemoryDatabaseRoot root)
    {
        var options = new DbContextOptionsBuilder<TestReadDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;
        return new TestReadDbContext(options, new TestConfigurationProvider());
    }

    private sealed class TestCapabilityRepository : Repository<TestEntity, int>
    {
        public TestCapabilityRepository(IDbContext context)
            : base(context) { }
    }

    private sealed class TestWriteRepository : WriteRepository<TestEntity>
    {
        public TestWriteRepository(IWriteDbContext context)
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

    private sealed class TestReadDbContext(
        DbContextOptions<TestReadDbContext> options,
        IEntityTypeConfigurationProvider provider)
        : ReadDbContext(options, provider, "test")
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestConfigurationProvider : IEntityTypeConfigurationProvider
    {
        public void Configure(ModelBuilder modelBuilder) => modelBuilder.Entity<TestEntity>();
    }

    private sealed class TestEntity : IIdEntity
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
    }
}
