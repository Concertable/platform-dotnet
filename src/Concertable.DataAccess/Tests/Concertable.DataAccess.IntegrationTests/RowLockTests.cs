using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Concertable.DataAccess.IntegrationTests;

[Collection(DataAccessCollection.Name)]
public sealed class RowLockTests : IAsyncLifetime
{

    private readonly DataAccessFixture fixture;

    public RowLockTests(DataAccessFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region LockRowsAsync

    [Fact]
    public async Task LockRowsAsync_MatchingRow_IsTrueAndBlocksAnotherWriter()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();

        var locked = await holder.References.LockRowsAsync(entity => entity.Id == id);

        Assert.True(locked);
        await AssertLockTimeoutAsync(() => RenameWithoutWaitingAsync(id));
    }

    [Fact]
    public async Task LockRowsAsync_NoMatchingRow_IsFalse()
    {
        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var locked = await context.References.LockRowsAsync(entity => entity.Id == -1);

        Assert.False(locked);
    }

    [Fact]
    public async Task LockRowsAsync_NoKeyUpdate_DoesNotBlockAChildInsert()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.References.LockRowsAsync(entity => entity.Id == id);

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
    public async Task LockRowsAsync_KeyShare_BlocksADeleteButNotAnUpdate()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.References.LockRowsAsync(entity => entity.Id == id, RowLock.KeyShare);

        await RenameWithoutWaitingAsync(id);

        await AssertLockTimeoutAsync(() => DeleteWithoutWaitingAsync(id));
    }

    [Fact]
    public async Task LockRowsAsync_LoadAfterTheLockIsGranted_SeesTheHoldersCommittedChildren()
    {
        var id = await InsertAsync("VENUE");
        await using var holder = fixture.CreateContext();
        await using var holderTransaction = await holder.Database.BeginTransactionAsync();
        await holder.References.LockRowsAsync(entity => entity.Id == id);
        holder.ReferenceNotes.Add(new ReferenceNoteEntity { ReferenceId = id, Text = "Added by the holder" });
        await holder.SaveChangesAsync();

        await using var waiter = fixture.CreateContext();
        await using var waiterTransaction = await waiter.Database.BeginTransactionAsync();
        var waiting = waiter.References.LockRowsAsync(entity => entity.Id == id);
        await WaitForALockWaiterAsync();
        await holderTransaction.CommitAsync();
        await waiting;
        var loaded = await waiter.References.Include(entity => entity.Notes).SingleAsync(entity => entity.Id == id);

        Assert.Single(loaded.Notes);
    }

    #endregion

    #region AcquireAdvisoryLockAsync

    [Fact]
    public async Task AcquireAdvisoryLockAsync_KeyHeldByAnotherTransaction_Blocks()
    {
        var key = Guid.NewGuid();
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.Database.AcquireAdvisoryLockAsync(TestLock.Reference, key);

        await AssertLockTimeoutAsync(
            () => AcquireWithoutWaitingAsync(database => database.AcquireAdvisoryLockAsync(TestLock.Reference, key)));
    }

    [Fact]
    public async Task AcquireSharedAdvisoryLockAsync_KeyHeldShared_IsGrantedButBlocksExclusive()
    {
        var key = Guid.NewGuid();
        await using var holder = fixture.CreateContext();
        await using var transaction = await holder.Database.BeginTransactionAsync();
        await holder.Database.AcquireSharedAdvisoryLockAsync(TestLock.Reference, key);

        await AcquireWithoutWaitingAsync(database => database.AcquireSharedAdvisoryLockAsync(TestLock.Reference, key));

        await AssertLockTimeoutAsync(
            () => AcquireWithoutWaitingAsync(database => database.AcquireAdvisoryLockAsync(TestLock.Reference, key)));
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

    private async Task WaitForALockWaiterAsync()
    {
        await using var observer = fixture.CreateContext();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (await observer.Database
                   .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_stat_activity WHERE wait_event_type = 'Lock'")
                   .SingleAsync(timeout.Token) == 0)
            await Task.Delay(25, timeout.Token);
    }

    private enum TestLock
    {
        Reference = 4242
    }
}
