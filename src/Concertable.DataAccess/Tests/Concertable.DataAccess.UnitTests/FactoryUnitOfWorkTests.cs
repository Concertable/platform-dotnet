using Concertable.DataAccess.Infrastructure;
using Concertable.Testing.Unit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.UnitTests;

public sealed class FactoryUnitOfWorkTests
{
    private readonly InMemoryDatabaseRoot root;
    private readonly string databaseName;
    private readonly TestDbContextFactory dbContextFactory;
    private readonly FactoryUnitOfWork<TestDbContext> unitOfWork;

    public FactoryUnitOfWorkTests()
    {
        (this.root, this.databaseName) = InMemoryDatabaseFactory.Create();
        this.dbContextFactory = new TestDbContextFactory(this.root, this.databaseName);
        this.unitOfWork = new FactoryUnitOfWork<TestDbContext>(this.dbContextFactory);
    }

    [Fact]
    public async Task ExecuteAsync_WriteOperation_PersistsThroughFactoryCreatedContext()
    {
        await this.unitOfWork.ExecuteAsync(context =>
        {
            context.Entities.Add(new TestEntity { Name = "Persisted" });
            return Task.CompletedTask;
        });

        Assert.Equal(1, this.dbContextFactory.AsyncCreateCount);
        Assert.True(this.dbContextFactory.Contexts[0].IsDisposed);

        await using var verificationContext = this.CreateVerificationContext();
        Assert.Equal("Persisted", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task ExecuteAsync_ResultOperation_UsesFreshFactoryCreatedContext()
    {
        var firstContextId = await this.unitOfWork.ExecuteAsync(context =>
        {
            context.Entities.Add(new TestEntity { Name = "First" });
            return Task.FromResult(context.ContextId.InstanceId);
        });
        var secondContextId = await this.unitOfWork.ExecuteAsync(context =>
        {
            context.Entities.Add(new TestEntity { Name = "Second" });
            return Task.FromResult(context.ContextId.InstanceId);
        });

        Assert.Equal(2, this.dbContextFactory.AsyncCreateCount);
        Assert.NotEqual(firstContextId, secondContextId);
        Assert.All(this.dbContextFactory.Contexts, context => Assert.True(context.IsDisposed));

        await using var verificationContext = this.CreateVerificationContext();
        Assert.Equal(2, await verificationContext.Entities.CountAsync());
    }

    private TestDbContext CreateVerificationContext() =>
        this.root.CreateContext(this.databaseName, options => new TestDbContext(options));

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        public bool IsDisposed { get; private set; }

        public override async ValueTask DisposeAsync()
        {
            this.IsDisposed = true;
            await base.DisposeAsync();
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>
    {
        private readonly InMemoryDatabaseRoot root;
        private readonly string databaseName;

        public TestDbContextFactory(InMemoryDatabaseRoot root, string databaseName)
        {
            this.root = root;
            this.databaseName = databaseName;
        }

        public int AsyncCreateCount { get; private set; }
        public List<TestDbContext> Contexts { get; } = [];

        public TestDbContext CreateDbContext() => throw new NotSupportedException();

        public Task<TestDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            this.AsyncCreateCount++;
            var context = this.root.CreateContext(this.databaseName, options => new TestDbContext(options));
            this.Contexts.Add(context);
            return Task.FromResult(context);
        }
    }

    private sealed class TestEntity
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
    }
}

