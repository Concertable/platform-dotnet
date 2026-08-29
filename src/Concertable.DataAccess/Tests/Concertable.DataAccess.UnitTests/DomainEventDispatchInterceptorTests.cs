using System.Reflection;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.UnitTests;

public sealed class DomainEventDispatchInterceptorTests : IDisposable
{
    private readonly SqliteConnection connection;

    public DomainEventDispatchInterceptorTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => connection.Dispose();

    [Fact]
    public async Task SaveChangesFailedAsync_ThenSuccessfulRetry_DispatchesOnlyTheRetriedEventsAndLeavesTheStackBalanced()
    {
        using (var seed = CreateContext())
        {
            seed.Aggregates.Add(new TestAggregate { Name = "Taken" });
            await seed.SaveChangesAsync();
        }

        var dispatcher = new RecordingDomainEventDispatcher();
        var interceptor = new DomainEventDispatchInterceptor(dispatcher, new TestDbContextAccessor());
        await using var context = CreateContext(interceptor);
        var conflicting = new TestAggregate { Name = "Taken" };
        context.Aggregates.Add(conflicting);
        conflicting.Raise(new TestDomainEvent("stale"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.Equal(0, PendingEventsStackCount(interceptor));

        context.Aggregates.Remove(conflicting);
        var succeeding = new TestAggregate { Name = "Unique" };
        context.Aggregates.Add(succeeding);
        succeeding.Raise(new TestDomainEvent("retried"));
        await context.SaveChangesAsync();

        Assert.Equal(0, PendingEventsStackCount(interceptor));
        var dispatched = Assert.Single(dispatcher.DispatchedBatches);
        var domainEvent = Assert.IsType<TestDomainEvent>(Assert.Single(dispatched));
        Assert.Equal("retried", domainEvent.Tag);
    }

    private static int PendingEventsStackCount(DomainEventDispatchInterceptor interceptor)
    {
        var field = typeof(DomainEventDispatchInterceptor)
            .GetField("pendingEventsStack", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var stack = (Stack<List<IDomainEvent>>)field.GetValue(interceptor)!;
        return stack.Count;
    }

    private TestDbContext CreateContext(DomainEventDispatchInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        return new TestDbContext(builder.Options);
    }

    private sealed record TestDomainEvent(string Tag) : IDomainEvent;

    private sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
    {
        public List<IReadOnlyList<IDomainEvent>> DispatchedBatches { get; } = [];

        public Task DispatchPreCommitAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
        {
            DispatchedBatches.Add(events.ToList());
            return Task.CompletedTask;
        }
    }

    private sealed class TestDbContextAccessor : IDbContextAccessor
    {
        public DbContext? Context { get; set; }
    }

    private sealed class TestAggregate : IEventRaiser
    {
        private readonly EventRaiser events = new();

        public int Id { get; private set; }
        public string Name { get; set; } = null!;

        public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
        public void ClearDomainEvents() => events.Clear();
        public void Raise(IDomainEvent domainEvent) => events.Raise(domainEvent);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestAggregate>().HasIndex(entity => entity.Name).IsUnique();
        }
    }
}
