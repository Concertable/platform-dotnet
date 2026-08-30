using System.Reflection;
using System.Linq.Expressions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using Concertable.Messaging.Domain;
using Concertable.Testing.Unit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.UnitTests;

public sealed class RepositoryTests
{
    private readonly InMemoryDatabaseRoot root;
    private readonly string databaseName;

    public RepositoryTests()
    {
        (this.root, this.databaseName) = InMemoryDatabaseFactory.Create();
    }

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
        await using var context = this.CreateContext();
        var repository = new TestWriteRepository(context);
        var entity = new TestEntity { Name = "Persisted" };

        Assert.Same(entity, await repository.AddAsync(entity));
        await repository.SaveChangesAsync();

        await using var verificationContext = this.CreateReadContext();
        Assert.Equal("Persisted", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task WriteRepository_TryInsertAsync_NoConflict_PersistsAndReturnsTrue()
    {
        await using var context = this.CreateContext();
        var repository = new TestWriteRepository(context);
        var entity = new TestEntity { Name = "Persisted" };

        var inserted = await repository.TryInsertAsync(entity);

        Assert.True(inserted);
        await using var verificationContext = this.CreateReadContext();
        Assert.Equal("Persisted", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task UnitOfWork_TrySaveChangesAsync_Success_PersistsAndReturnsTrue()
    {
        await using var context = this.CreateContext();
        await context.AddAsync(new TestEntity { Name = "Persisted" });
        IUnitOfWork<TestDbContext> unitOfWork = new UnitOfWork<TestDbContext>(context);

        var saved = await unitOfWork.TrySaveChangesAsync();

        Assert.True(saved);
        Assert.Equal("Persisted", (await context.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task UnitOfWork_TrySaveChangesAsync_ConcurrencyFailure_ClearsChangeTracker()
    {
        await using (var seed = this.CreateContext())
        {
            await seed.AddRangeAsync(
                new TestEntity { Name = "Original" },
                new TestEntity { Name = "Unrelated" });
            await seed.SaveChangesAsync();
        }
        await using var winnerContext = this.CreateContext();
        await using var loserContext = this.CreateContext();
        var winner = await winnerContext.Entities.SingleAsync(entity => entity.Name == "Original");
        var loser = await loserContext.Entities.SingleAsync(entity => entity.Name == "Original");
        _ = await loserContext.Entities.SingleAsync(entity => entity.Name == "Unrelated");
        winner.Name = "Winner";
        loser.Name = "Loser";
        await winnerContext.SaveChangesAsync();
        IUnitOfWork<TestDbContext> unitOfWork = new UnitOfWork<TestDbContext>(loserContext);

        var saved = await unitOfWork.TrySaveChangesAsync();

        Assert.False(saved);
        Assert.Empty(loserContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task UnitOfWork_TrySaveChangesAsync_UpdateFailure_ClearsChangeTracker()
    {
        var options = new DbContextOptionsBuilder<FailingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new FailingDbContext(options);
        await context.AddAsync(new TestEntity { Name = "Failed" });
        IUnitOfWork<FailingDbContext> unitOfWork = new UnitOfWork<FailingDbContext>(context);

        var saved = await unitOfWork.TrySaveChangesAsync();

        Assert.False(saved);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Repository_ReadThenSave_PreservesOneTrackedUnitOfWork()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        var entity = new TestEntity { Name = "Original" };
        await repository.InsertAsync(entity);

        var loaded = await repository.GetByIdAsync(entity.Id);
        loaded!.Name = "Updated";
        await repository.SaveChangesAsync();

        Assert.Same(entity, loaded);
        await using var verificationContext = this.CreateContext(QueryTrackingBehavior.NoTracking);
        Assert.Equal("Updated", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task ReadRepository_DedicatedNoTrackingContext_IsolatedFromTrackedChanges()
    {
        await using var trackedContext = this.CreateContext();
        var trackedRepository = new TestCapabilityRepository(trackedContext);
        var entity = new TestEntity { Name = "Persisted" };
        await trackedRepository.InsertAsync(entity);
        entity.Name = "Unsaved";
        await using var readContext = this.CreateReadContext();
        var readRepository = new TestReadRepository(readContext);

        var isolated = await readRepository.GetByIdAsync(entity.Id);

        Assert.Equal("Persisted", isolated!.Name);
        Assert.NotSame(entity, isolated);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Repository_GetByIdAsync_WithShapeSpecification_LoadsNestedIncludedNavigation()
    {
        int id;
        await using (var seed = this.CreateContext())
        {
            var entity = new TestEntity
            {
                Name = "Included",
                Detail = new TestEntityDetail
                {
                    Value = "Loaded",
                    Owner = new TestEntityOwner { Name = "Nested" }
                }
            };
            await seed.AddAsync(entity);
            await seed.SaveChangesAsync();
            id = entity.Id;
        }

        await using var context = this.CreateContext(QueryTrackingBehavior.NoTracking);
        var repository = new TestCapabilityRepository(context);

        var result = await repository.GetByIdAsync(id, new TestEntityWithDetailOwnerSpecification());

        Assert.NotNull(result);
        Assert.NotNull(result.Detail);
        Assert.Equal("Loaded", result.Detail.Value);
        Assert.NotNull(result.Detail.Owner);
        Assert.Equal("Nested", result.Detail.Owner.Name);
    }

    [Fact]
    public async Task Repository_GetByIdAsync_WithProjectionSpecification_ProjectsTheMatchingEntity()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        var entity = new TestEntity { Name = "Projected" };
        await repository.InsertAsync(entity);

        var result = await repository.GetByIdAsync(entity.Id, new TestEntityNameSpecification());

        Assert.Equal(new TestEntityName(entity.Id, "Projected"), result);
    }

    [Fact]
    public async Task Repository_GetAllAsync_WithOrderedSpecification_AppliesAllOrders()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "B" });
        await repository.InsertAsync(new TestEntity { Name = "A" });
        await repository.InsertAsync(new TestEntity { Name = "C" });

        var result = await repository.GetAllAsync(new TestEntitiesByNameSpecification());

        Assert.Equal(["A", "B", "C"], result.Select(entity => entity.Name));
    }

    [Fact]
    public async Task Repository_GetAllAsync_WithOrderedProjectionSpecification_AppliesOrderBeforeProjection()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "B" });
        await repository.InsertAsync(new TestEntity { Name = "A" });

        var result = await repository.GetAllAsync(new TestEntityNamesByNameSpecification());

        Assert.Equal(["A", "B"], result.Select(entity => entity.Name));
    }

    [Fact]
    public async Task Repository_GetAllAsync_WithDateOrderSpecification_AppliesDateOrder()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "Later", CreatedAt = new DateTime(2026, 9, 2) });
        await repository.InsertAsync(new TestEntity { Name = "Earlier", CreatedAt = new DateTime(2026, 9, 1) });

        var result = await repository.GetAllAsync(new TestEntitiesByCreatedAtSpecification());

        Assert.Equal(["Earlier", "Later"], result.Select(entity => entity.Name));
    }

    [Fact]
    public async Task Repository_GetAllAsync_WithCancelledToken_CancelsTheDatabaseOperation()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAllAsync(new TestEntityWithDetailSpecification(), cancellation.Token));
    }

    [Fact]
    public async Task Repository_GetAllAsync_DoesNotApplyAPredicateImplementedByAShapeSpecification()
    {
        await using var context = this.CreateContext();
        var repository = new TestCapabilityRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "Returned" });

        ISpecification<TestEntity> specification = new ShapeAndPredicateSpecification();
        var result = await repository.GetAllAsync(specification);

        Assert.Single(result);
    }

    [Fact]
    public async Task ReadDbContext_SaveOverloads_RejectWrites()
    {
        using var context = this.CreateReadContext();

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Throws<InvalidOperationException>(() => context.SaveChanges(acceptAllChangesOnSuccess: false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync(acceptAllChangesOnSuccess: false));
    }

    [Fact]
    public void ReadDbContext_Model_ExcludesMessagingEntities()
    {
        using var context = this.CreateReadContext();

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

    private TestDbContext CreateContext(QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll) =>
        this.root.CreateContext<TestDbContext>(this.databaseName, options => new TestDbContext(options), trackingBehavior);

    private TestReadDbContext CreateReadContext() =>
        this.root.CreateContext<TestReadDbContext>(
            this.databaseName, options => new TestReadDbContext(options, new TestConfigurationProvider()));

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

    private sealed class TestEntityWithDetailSpecification : Specification<TestEntity>
    {
        public TestEntityWithDetailSpecification()
        {
            this.Include(entity => entity.Detail);
        }
    }

    private sealed class TestEntityWithDetailOwnerSpecification : Specification<TestEntity>
    {
        public TestEntityWithDetailOwnerSpecification()
        {
            this.Include(entity => entity.Detail!.Owner);
        }
    }

    private sealed class TestEntityNameSpecification : Specification<TestEntity, TestEntityName>
    {
        public TestEntityNameSpecification()
            : base(entity => new TestEntityName(entity.Id, entity.Name)) { }
    }

    private sealed class TestEntitiesByNameSpecification : Specification<TestEntity>, IOrderedSpecification<TestEntity>
    {
        public IReadOnlyList<SpecificationOrder<TestEntity>> Orders => this.RegisteredOrders;

        public TestEntitiesByNameSpecification()
        {
            this.OrderBy(entity => entity.Name);
            this.ThenBy(entity => entity.Id);
        }
    }

    private sealed class TestEntityNamesByNameSpecification
        : Specification<TestEntity, TestEntityName>, IOrderedSpecification<TestEntity, TestEntityName>
    {
        public IReadOnlyList<SpecificationOrder<TestEntity>> Orders => this.RegisteredOrders;

        public TestEntityNamesByNameSpecification()
            : base(entity => new TestEntityName(entity.Id, entity.Name))
        {
            this.OrderBy(entity => entity.Name);
        }
    }

    private sealed class TestEntitiesByCreatedAtSpecification : Specification<TestEntity>, IOrderedSpecification<TestEntity>
    {
        public IReadOnlyList<SpecificationOrder<TestEntity>> Orders => this.RegisteredOrders;

        public TestEntitiesByCreatedAtSpecification()
        {
            this.OrderBy(entity => entity.CreatedAt);
        }
    }

    private sealed class ShapeAndPredicateSpecification : Specification<TestEntity>, IPredicateSpecification<TestEntity>
    {
        public Expression<Func<TestEntity, bool>> ToExpression() => entity => entity.Name == "Excluded";
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
        public DbSet<TestEntityDetail> Details => Set<TestEntityDetail>();
        public DbSet<TestEntityOwner> Owners => Set<TestEntityOwner>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity>().Property(entity => entity.Name).IsConcurrencyToken();
            modelBuilder.Entity<TestEntity>()
                .HasOne(entity => entity.Detail)
                .WithOne(detail => detail.Entity)
                .HasForeignKey<TestEntityDetail>(detail => detail.EntityId);
            modelBuilder.Entity<TestEntityDetail>()
                .HasOne(detail => detail.Owner)
                .WithOne(owner => owner.Detail)
                .HasForeignKey<TestEntityOwner>(owner => owner.DetailId);
        }
    }

    private sealed class FailingDbContext(DbContextOptions<FailingDbContext> options) : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException();
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
        public DateTime CreatedAt { get; set; }
        public TestEntityDetail? Detail { get; set; }
    }

    private sealed class TestEntityDetail
    {
        public int Id { get; private set; }
        public int EntityId { get; set; }
        public TestEntity Entity { get; set; } = null!;
        public string Value { get; set; } = null!;
        public TestEntityOwner? Owner { get; set; }
    }

    private sealed class TestEntityOwner
    {
        public int Id { get; private set; }
        public int DetailId { get; set; }
        public TestEntityDetail Detail { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private sealed record TestEntityName(int Id, string Name);
}
