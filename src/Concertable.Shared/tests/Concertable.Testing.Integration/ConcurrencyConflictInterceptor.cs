using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.Testing.Integration;

/// <summary>
/// Turns a race into a deterministic one. Armed with a competing change and the entity type whose update
/// should lose, it commits that competing change — on its own connection, outside the operation's ambient
/// transaction — in the window between the operation reading the row and its UPDATE reaching the server.
/// The operation's rowversion predicate then matches nothing, so the store raises the concurrency
/// exception a real interleaving would have produced, at a point the test chose rather than one the
/// scheduler chose.
/// </summary>
public sealed class ConcurrencyConflictInterceptor : SaveChangesInterceptor, IResettable
{
    private Func<Task>? competingChange;
    private Type? losingEntityType;

    /// <summary>How many times a conflict was actually forced — assert on this so a test cannot pass by
    /// silently never reaching the retry it claims to cover.</summary>
    public int ForcedConflicts { get; private set; }

    public void ArmOnce<TEntity>(Func<Task> competingChange)
        where TEntity : class
    {
        this.losingEntityType = typeof(TEntity);
        this.competingChange = competingChange;
    }

    public void Reset()
    {
        this.competingChange = null;
        this.losingEntityType = null;
        this.ForcedConflicts = 0;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (this.competingChange is { } pending &&
            this.losingEntityType is { } entityType &&
            HasPendingUpdate(eventData.Context!, entityType))
        {
            this.competingChange = null;
            this.ForcedConflicts++;
            using var suppressed = new TransactionScope(
                TransactionScopeOption.Suppress,
                TransactionScopeAsyncFlowOption.Enabled);
            await pending();
            suppressed.Complete();
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool HasPendingUpdate(DbContext context, Type entityType) =>
        context.ChangeTracker.Entries().Any(entry =>
            entry.State == EntityState.Modified && entityType.IsInstanceOfType(entry.Entity));
}
