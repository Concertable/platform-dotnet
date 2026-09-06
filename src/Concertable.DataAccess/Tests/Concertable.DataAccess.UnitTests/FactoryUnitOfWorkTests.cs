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
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();

        dbContextFactory = new TestDbContextFactory(connection);
        unitOfWork = new FactoryUnitOfWork<TestDbContext>(dbContextFactory);
    }

    [Fact]
    public async Task ExecuteAsync_WriteOperation_PersistsThroughFactoryCreatedContext()
    {
        await unitOfWork.ExecuteAsync(context =>
        {
            context.Entities.Add(new TestEntity { Name = "Persisted" });
            return Task.CompletedTask;
        });

        Assert.Equal(1, dbContextFactory.AsyncCreateCount);
        Assert.True(dbContextFactory.Contexts[0].IsDisposed);

        await using var verificationContext = CreateVerificationContext();
        Assert.Equal("Persisted", (await verificationContext.Entities.SingleAsync()).Name);
    }

    [Fact]
    public async Task ExecuteAsync_ResultOperation_UsesFreshFactoryCreatedContext()
    {
        var firstContextId = await unitOfWork.ExecuteAsync(context =>
        {
            context.Entities.Add(new TestEntity { Name = "First" });
            return Task.FromResult(context.ContextId.InstanceId);
        });
        var secondContextId = await unitOfWork.ExecuteAsync(context =>
        {
            context.Entities.Add(new TestEntity { Name = "Second" });
            return Task.FromResult(context.ContextId.InstanceId);
        });

        Assert.Equal(2, dbContextFactory.AsyncCreateCount);
        Assert.NotEqual(firstContextId, secondContextId);
        Assert.All(dbContextFactory.Contexts, context => Assert.True(context.IsDisposed));

        await using var verificationContext = CreateVerificationContext();
        Assert.Equal(2, await verificationContext.Entities.CountAsync());
    }

    [Fact]
    public async Task TryExecuteAsync_ExpectedFailure_DisposesTheContextBeforeClassifying()
    {
        bool? disposedWhenClassified = null;

        var outcome = await unitOfWork.TryExecuteAsync<string>(
            _ => throw new DbUpdateException(),
            static _ => true,
            _ =>
            {
                disposedWhenClassified = dbContextFactory.Contexts[0].IsDisposed;
                return Task.FromResult("classified");
            });

        Assert.Equal("classified", outcome);
        Assert.True(disposedWhenClassified);
    }

    [Fact]
    public async Task TryExecuteAsync_ExpectedFailure_RollsBackTheOperationsWrites()
    {
        await unitOfWork.TryExecuteAsync<string>(
            context =>
            {
                context.Entities.Add(new TestEntity { Name = "Rolled back" });
                throw new DbUpdateException();
            },
            static _ => true,
            _ => Task.FromResult("classified"));

        await using var verificationContext = CreateVerificationContext();
        Assert.Empty(await verificationContext.Entities.ToListAsync());
    }

    [Fact]
    public async Task TryExecuteAsync_RejectedFailure_Propagates()
    {
        await Assert.ThrowsAsync<DbUpdateException>(
            () => unitOfWork.TryExecuteAsync<string>(
                _ => throw new DbUpdateException(),
                static _ => false,
                _ => Task.FromResult("classified")));
    }

    [Fact]
    public async Task TryExecuteAsync_Success_PersistsAndNeverClassifies()
    {
        var classified = false;

        var outcome = await unitOfWork.TryExecuteAsync(
            context =>
            {
                context.Entities.Add(new TestEntity { Name = "Persisted" });
                return Task.FromResult("committed");
            },
            static _ => true,
            _ =>
            {
                classified = true;
                return Task.FromResult("classified");
            });

        Assert.Equal("committed", outcome);
        Assert.False(classified);

        await using var verificationContext = CreateVerificationContext();
        Assert.Equal("Persisted", (await verificationContext.Entities.SingleAsync()).Name);
    }

    public void Dispose() => connection.Dispose();

    private TestDbContext CreateVerificationContext() => CreateContext();

    private TestDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();

        public bool IsDisposed { get; private set; }

        public override async ValueTask DisposeAsync()
        {
            IsDisposed = true;
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

        public TestDbContext CreateDbContext() => CreateContext();

        public Task<TestDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            AsyncCreateCount++;
            var context = CreateContext();
            Contexts.Add(context);
            return Task.FromResult(context);
        }

        private TestDbContext CreateContext() =>
            new(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .Options);
    }

    private sealed class TestEntity
    {
        public int Id { get; private set; }
        public string Name { get; set; } = null!;
    }
}
