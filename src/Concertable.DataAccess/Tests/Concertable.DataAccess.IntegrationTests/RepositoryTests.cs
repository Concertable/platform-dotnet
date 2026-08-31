using System.Linq.Expressions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.IntegrationTests;

public sealed class RepositoryTests : IDisposable
{
    private readonly SqliteConnection connection;

    public RepositoryTests()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShapeSpecification_LoadsNestedIncludedNavigation()
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
        var repository = new TestRepository(context);

        var result = await repository.GetByIdAsync(id, new TestEntitySpecification().Include(entity => entity.Detail!.Owner));

        Assert.NotNull(result);
        Assert.NotNull(result.Detail);
        Assert.Equal("Loaded", result.Detail.Value);
        Assert.NotNull(result.Detail.Owner);
        Assert.Equal("Nested", result.Detail.Owner.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ThenIncludeThroughCollection_LoadsTheNestedGraph()
    {
        int id;
        await using (var seed = this.CreateContext())
        {
            var entity = new TestEntity
            {
                Name = "Owner",
                Items =
                [
                    new TestEntityItem { Label = "First", Tag = new TestEntityTag { Name = "Tagged" } }
                ]
            };
            await seed.AddAsync(entity);
            await seed.SaveChangesAsync();
            id = entity.Id;
        }

        await using var context = this.CreateContext(QueryTrackingBehavior.NoTracking);
        var repository = new TestRepository(context);

        var result = await repository.GetByIdAsync(id, new TestEntitySpecification().Include(entity => entity.Items).ThenInclude(item => item.Tag));

        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal("First", item.Label);
        Assert.NotNull(item.Tag);
        Assert.Equal("Tagged", item.Tag.Name);
    }

    [Fact]
    public async Task GetByIdAsync_UnrequestedNavigation_RemainsUnloaded()
    {
        int id;
        await using (var seed = this.CreateContext())
        {
            var entity = new TestEntity
            {
                Name = "Owner",
                Detail = new TestEntityDetail { Value = "Loaded" },
                Items = [new TestEntityItem { Label = "First" }]
            };
            await seed.AddAsync(entity);
            await seed.SaveChangesAsync();
            id = entity.Id;
        }

        await using var context = this.CreateContext(QueryTrackingBehavior.NoTracking);
        var repository = new TestRepository(context);

        var result = await repository.GetByIdAsync(id, new TestEntitySpecification().Include(entity => entity.Detail));

        Assert.NotNull(result);
        Assert.NotNull(result.Detail);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetByIdAsync_RepeatedFluentIncludes_AreIdempotent()
    {
        int id;
        await using (var seed = this.CreateContext())
        {
            var entity = new TestEntity
            {
                Name = "Owner",
                Detail = new TestEntityDetail { Value = "Loaded" }
            };
            await seed.AddAsync(entity);
            await seed.SaveChangesAsync();
            id = entity.Id;
        }

        await using var context = this.CreateContext(QueryTrackingBehavior.NoTracking);
        var repository = new TestRepository(context);

        var specification = new TestEntitySpecification().Include(entity => entity.Detail).Include(entity => entity.Detail);
        var result = await repository.GetByIdAsync(id, specification);

        Assert.NotNull(result);
        Assert.NotNull(result.Detail);
    }

    [Fact]
    public async Task GetByIdAsync_ProjectionSpecification_ProjectsMatchingEntity()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        var entity = new TestEntity { Name = "Projected" };
        await repository.InsertAsync(entity);

        var result = await repository.GetByIdAsync(entity.Id, new TestEntitySpecification().Select(entity => new TestEntityName(entity.Id, entity.Name)));

        Assert.Equal(new TestEntityName(entity.Id, "Projected"), result);
    }

    [Fact]
    public async Task GetByIdAsync_ValueProjection_ProjectsMatchingEntity()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        var entity = new TestEntity { Name = "Projected" };
        await repository.InsertAsync(entity);

        var result = await repository.GetByIdAsync(
            entity.Id,
            new TestEntitySpecification().Select(candidate => candidate.Id));

        Assert.Equal(entity.Id, result);
    }

    [Fact]
    public async Task GetByIdAsync_NullableValueProjection_ProjectsTheColumn()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        var entity = new TestEntity { Name = "Dated", CancelledAt = new DateTime(2026, 9, 3) };
        await repository.InsertAsync(entity);

        var result = await repository.GetByIdAsync(
            entity.Id,
            new TestEntitySpecification().Select(candidate => candidate.CancelledAt));

        Assert.Equal(new DateTime(2026, 9, 3), result);
    }

    [Fact]
    public async Task GetByIdAsync_ValueProjection_MissingRow_ReturnsNullRatherThanDefault()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);

        var result = await repository.GetByIdAsync(
            404,
            new TestEntitySpecification().Select(candidate => candidate.Id));

        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_OrderedSpecification_AppliesAllOrders()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "B" });
        await repository.InsertAsync(new TestEntity { Name = "A" });
        await repository.InsertAsync(new TestEntity { Name = "C" });

        var result = await repository.GetAllAsync(new TestEntitySpecification().OrderBy(entity => entity.Name).ThenBy(entity => entity.Id));

        Assert.Equal(["A", "B", "C"], result.Select(entity => entity.Name));
    }

    [Fact]
    public async Task GetAllAsync_OrderedProjectionSpecification_AppliesOrderBeforeProjection()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "B" });
        await repository.InsertAsync(new TestEntity { Name = "A" });

        var result = await repository.GetAllAsync(new TestEntitySpecification().OrderBy(entity => entity.Name).Select(entity => new TestEntityName(entity.Id, entity.Name)));

        Assert.Equal(["A", "B"], result.Select(entity => entity.Name));
    }

    [Fact]
    public async Task GetAllAsync_DateOrderSpecification_AppliesDateOrder()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "Later", CreatedAt = new DateTime(2026, 9, 2) });
        await repository.InsertAsync(new TestEntity { Name = "Earlier", CreatedAt = new DateTime(2026, 9, 1) });

        var result = await repository.GetAllAsync(new TestEntitySpecification().OrderBy(entity => entity.CreatedAt));

        Assert.Equal(["Earlier", "Later"], result.Select(entity => entity.Name));
    }

    [Fact]
    public async Task GetAllAsync_CancelledToken_CancelsDatabaseOperation()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAllAsync(new TestEntitySpecification().Include(entity => entity.Detail), cancellation.Token));
    }

    [Fact]
    public async Task GetAllAsync_ShapeSpecificationAlsoImplementingPredicate_DoesNotApplyPredicate()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);
        await repository.InsertAsync(new TestEntity { Name = "Returned" });

        ISpecification<TestEntity> specification = new ShapeAndPredicateSpecification();
        var result = await repository.GetAllAsync(specification);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_NonMemberInclude_ThrowsArgumentException()
    {
        await using var context = this.CreateContext();
        var repository = new TestRepository(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetAllAsync(new TestEntitySpecification().Include(entity => entity.Name.ToUpper())));
    }

    #endregion

    public void Dispose()
    {
        this.connection.Dispose();
    }

    private TestDbContext CreateContext(QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(this.connection)
            .UseQueryTrackingBehavior(trackingBehavior)
            .Options;
        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TestRepository : Repository<TestEntity, int>
    {
        public TestRepository(IDbContext context)
            : base(context) { }
    }

    private sealed class TestEntitySpecification : SpecificationBuilder<TestEntity>;

    private sealed class ShapeAndPredicateSpecification : Specification<TestEntity>, IPredicateSpecification<TestEntity>
    {
        public Expression<Func<TestEntity, bool>> ToExpression() => entity => entity.Name == "Excluded";
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
        public DbSet<TestEntityDetail> Details => Set<TestEntityDetail>();
        public DbSet<TestEntityOwner> Owners => Set<TestEntityOwner>();
        public DbSet<TestEntityItem> Items => Set<TestEntityItem>();
        public DbSet<TestEntityTag> Tags => Set<TestEntityTag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity>()
                .HasOne(entity => entity.Detail)
                .WithOne(detail => detail.Entity)
                .HasForeignKey<TestEntityDetail>(detail => detail.EntityId);
            modelBuilder.Entity<TestEntityDetail>()
                .HasOne(detail => detail.Owner)
                .WithOne(owner => owner.Detail)
                .HasForeignKey<TestEntityOwner>(owner => owner.DetailId);
            modelBuilder.Entity<TestEntity>()
                .HasMany(entity => entity.Items)
                .WithOne(item => item.Entity)
                .HasForeignKey(item => item.EntityId);
            modelBuilder.Entity<TestEntityItem>()
                .HasOne(item => item.Tag)
                .WithMany()
                .HasForeignKey(item => item.TagId);
        }
    }

    private sealed class TestEntity : IIdEntity
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public TestEntityDetail? Detail { get; set; }
        public ICollection<TestEntityItem> Items { get; set; } = [];
    }

    private sealed class TestEntityItem
    {
        public int Id { get; private set; }
        public int EntityId { get; set; }
        public TestEntity Entity { get; set; } = null!;
        public string Label { get; set; } = null!;
        public int? TagId { get; set; }
        public TestEntityTag? Tag { get; set; }
    }

    private sealed class TestEntityTag
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
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
