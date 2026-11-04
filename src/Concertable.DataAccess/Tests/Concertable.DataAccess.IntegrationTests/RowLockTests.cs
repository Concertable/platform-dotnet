using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Concertable.DataAccess.IntegrationTests;

[Collection(DataAccessCollection.Name)]
public sealed class RowLockTests : IAsyncLifetime
{
    private const int LockNamespace = 4242;

    private readonly DataAccessFixture fixture;

    public RowLockTests(DataAccessFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region WithRowLock

    [Fact]
    public async Task WithRowLock_RowLoadedInAnOpenTransaction_BlocksAnotherWriter()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();

        var locked = await holder.References.WithRowLock().SingleAsync(entity => entity.Id == id);

        Assert.Equal("VENUE", locked.Code);
        await AssertLockTimeoutAsync(() => RenameWithoutWaitingAsync(id));
    }

    [Fact]
    public async Task WithRowLock_IncludeOfAnOptionalCollection_LocksTheRootRow()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();

        var locked = await holder.References
            .WithRowLock()
            .Include(entity => entity.Notes)
            .SingleAsync(entity => entity.Id == id);

        Assert.Empty(locked.Notes);
        await AssertLockTimeoutAsync(() => RenameWithoutWaitingAsync(id));
    }

    [Fact]
    public async Task WithRowLock_ChildInsertReferencingTheLockedRow_IsNotBlocked()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.References.WithRowLock().SingleAsync(entity => entity.Id == id);

        await using var writer = fixture.CreateContext();
        await using var writerTransaction = await writer.Database.BeginTransactionAsync();
        await writer.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'");
        writer.ReferenceNotes.Add(new ReferenceNoteEntity { ReferenceId = id, Text = "Load-in at six" });
        await writer.SaveChangesAsync();
        await writerTransaction.CommitAsync();

        await using var probe = fixture.CreateContext();
        Assert.Single(await probe.ReferenceNotes.Where(note => note.ReferenceId == id).ToListAsync());
    }

    [Fact]
    public async Task WithRowLock_Omitted_LeavesTheRowWritable()
    {
        var id = await InsertAsync("VENUE");
        await using var reader = fixture.CreateContext();
        await using var transaction = await reader.Database.BeginTransactionAsync();
        await reader.References.SingleAsync(entity => entity.Id == id);

        await RenameWithoutWaitingAsync(id);

        await using var probe = fixture.CreateContext();
        Assert.Equal("Renamed", (await probe.References.SingleAsync(entity => entity.Id == id)).Name);
    }

    [Fact]
    public async Task WithRowLock_KeyShare_BlocksADeleteButNotAnUpdate()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.References.WithRowLock(RowLock.KeyShare).SingleAsync(entity => entity.Id == id);

        await RenameWithoutWaitingAsync(id);

        await AssertLockTimeoutAsync(() => DeleteWithoutWaitingAsync(id));
    }

    #endregion

    #region AcquireAdvisoryLockAsync

    [Fact]
    public async Task AcquireAdvisoryLockAsync_KeyHeldByAnotherTransaction_Blocks()
    {
        var key = Guid.NewGuid();
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.Database.AcquireAdvisoryLockAsync(LockNamespace, key);

        await AssertLockTimeoutAsync(
            () => AcquireWithoutWaitingAsync(database => database.AcquireAdvisoryLockAsync(LockNamespace, key)));
    }

    [Fact]
    public async Task AcquireSharedAdvisoryLockAsync_KeyHeldShared_IsGrantedButBlocksExclusive()
    {
        var key = Guid.NewGuid();
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.Database.AcquireSharedAdvisoryLockAsync(LockNamespace, key);

        await AcquireWithoutWaitingAsync(database => database.AcquireSharedAdvisoryLockAsync(LockNamespace, key));

        await AssertLockTimeoutAsync(
            () => AcquireWithoutWaitingAsync(database => database.AcquireAdvisoryLockAsync(LockNamespace, key)));
    }

    #endregion

    private async Task<int> InsertAsync(string code)
    {
        await using var context = fixture.CreateContext();
        var reference = new ReferenceEntity { Code = code, Name = code };
        context.References.Add(reference);
        await context.SaveChangesAsync();
        return reference.Id;
    }

    private async Task RenameWithoutWaitingAsync(int id)
    {
        await using var writer = fixture.CreateContext();
        await using var transaction = await writer.Database.BeginTransactionAsync();
        await writer.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'");
        await writer.References
            .Where(entity => entity.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(entity => entity.Name, "Renamed"));
        await transaction.CommitAsync();
    }

    private async Task DeleteWithoutWaitingAsync(int id)
    {
        await using var writer = fixture.CreateContext();
        await using var transaction = await writer.Database.BeginTransactionAsync();
        await writer.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'");
        await writer.References.Where(entity => entity.Id == id).ExecuteDeleteAsync();
        await transaction.CommitAsync();
    }

    private async Task AcquireWithoutWaitingAsync(
        Func<DatabaseFacade, Task> acquire)
    {
        await using var contender = fixture.CreateContext();
        await using var transaction = await contender.Database.BeginTransactionAsync();
        await contender.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '200ms'");
        await acquire(contender.Database);
        await transaction.CommitAsync();
    }

    private static async Task AssertLockTimeoutAsync(Func<Task> contend)
    {
        var failure = await Record.ExceptionAsync(contend);

        var postgres = Assert.IsType<PostgresException>(failure as PostgresException ?? failure?.InnerException);
        Assert.Equal(PostgresErrorCodes.LockNotAvailable, postgres.SqlState);
    }
}
