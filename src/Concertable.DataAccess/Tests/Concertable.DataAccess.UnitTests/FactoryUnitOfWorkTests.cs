using Concertable.DataAccess.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.UnitTests;

public sealed class FactoryUnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly TestDbContextFactory dbContextFactory;
    private readonly FactoryUnitOfWork<TestDbContext> unitOfWork;

    public FactoryUnitOfWorkTests()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();

        using var context = this.CreateContext();
        context.Database.EnsureCreated();

        this.dbContextFactory = new TestDbContextFactory(this.connection);
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

    public void Dispose() => this.connection.Dispose();

    private TestDbContext CreateVerificationContext() => this.CreateContext();

    private TestDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(this.connection)
                .Options);

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
        private readonly SqliteConnection connection;

        public TestDbContextFactory(SqliteConnection connection)
        {
            this.connection = connection;
        }

        public int AsyncCreateCount { get; private set; }
        public List<TestDbContext> Contexts { get; } = [];

        public TestDbContext CreateDbContext() => this.CreateContext();

        public Task<TestDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            this.AsyncCreateCount++;
            var context = this.CreateContext();
            this.Contexts.Add(context);
            return Task.FromResult(context);
        }

        private TestDbContext CreateContext() =>
            new(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(this.connection)
                    .Options);
    }

    private sealed class TestEntity
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
    }
}
