using System.Transactions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.DataAccess.UnitTests;

public sealed class UnitOfWorkBehaviorTests
{
    private readonly ThrowingUnitOfWork unitOfWork;
    private readonly UnitOfWorkBehavior<TestDbContext> behavior;

    public UnitOfWorkBehaviorTests()
    {
        this.unitOfWork = new ThrowingUnitOfWork();
        this.behavior = new UnitOfWorkBehavior<TestDbContext>(this.unitOfWork);
    }

    [Fact]
    public async Task TryExecuteAsync_ExpectedFailure_ClassifiesOutsideTheAmbientTransaction()
    {
        Transaction? duringAction = null;
        Transaction? duringClassification = null;

        var outcome = await this.behavior.TryExecuteAsync(
            () =>
            {
                duringAction = Transaction.Current;
                return Task.FromResult("committed");
            },
            static _ => true,
            _ =>
            {
                duringClassification = Transaction.Current;
                return Task.FromResult("classified");
            });

        Assert.Equal("classified", outcome);
        Assert.NotNull(duringAction);
        Assert.Null(duringClassification);
    }

    [Fact]
    public async Task TryExecuteAsync_ExpectedFailure_DoesNotCompleteTheScope()
    {
        var completed = true;

        await this.behavior.TryExecuteAsync(
            () => Task.FromResult("committed"),
            static _ => true,
            _ =>
            {
                completed = Transaction.Current is not null;
                return Task.FromResult("classified");
            });

        Assert.False(completed);
    }

    [Fact]
    public async Task TryExecuteAsync_RejectedFailure_Propagates()
    {
        await Assert.ThrowsAsync<DbUpdateException>(
            () => this.behavior.TryExecuteAsync(
                () => Task.FromResult("committed"),
                static _ => false,
                _ => Task.FromResult("classified")));
    }

    [Fact]
    public async Task TryExecuteAsync_Success_ReturnsTheValueAndNeverClassifies()
    {
        this.unitOfWork.Throw = false;
        var classified = false;

        var outcome = await this.behavior.TryExecuteAsync(
            () => Task.FromResult("committed"),
            static _ => true,
            _ =>
            {
                classified = true;
                return Task.FromResult("classified");
            });

        Assert.Equal("committed", outcome);
        Assert.False(classified);
    }

    [Fact]
    public async Task TryExecuteAsync_Cancellation_PropagatesWithoutClassifying()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var classified = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.behavior.TryExecuteAsync(
                () => Task.FromCanceled<string>(cancellation.Token),
                static _ => true,
                _ =>
                {
                    classified = true;
                    return Task.FromResult("classified");
                },
                cancellation.Token));

        Assert.False(classified);
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork<TestDbContext>
    {
        public bool Throw { get; set; } = true;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            this.Throw ? throw new DbUpdateException() : Task.CompletedTask;

        public Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> TrySaveChangesAsync(
            Func<DbUpdateException, bool> isExpected,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<TResult> ExecuteAsync<TResult>(
            Func<Task<TResult>> operation,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContextBase(options);
}
