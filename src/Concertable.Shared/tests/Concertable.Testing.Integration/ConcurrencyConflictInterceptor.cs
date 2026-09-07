using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.Testing.Integration;

/// <summary>
/// Turns a race into a deterministic one. Armed with a competing change and the entity type whose update
/// should lose, it commits that competing change in the window between the operation reading that entity's
/// row and the operation's own UPDATE reaching the server. The operation's rowversion predicate then matches
/// nothing, so the store raises the concurrency exception a real interleaving would have produced, at a point
/// the test chose rather than one the scheduler chose.
/// </summary>
public sealed class ConcurrencyConflictInterceptor : IDbCommandInterceptor, ISaveChangesInterceptor, IResettable
{
    private Func<Task>? competingChange;
    private Type? losingEntityType;
    private bool committed;

    /// <summary>How many times a conflict was actually forced — assert on this so a test cannot pass by
    /// silently never reaching the retry it claims to cover.</summary>
    public int ForcedConflicts { get; private set; }

    public void ArmOnce<TEntity>(Func<Task> competingChange)
        where TEntity : class
    {
        losingEntityType = typeof(TEntity);
        this.competingChange = competingChange;
    }

    public void Reset()
    {
        competingChange = null;
        losingEntityType = null;
        committed = false;
        ForcedConflicts = 0;
    }

    // The read, not the save, is the safe window: by its own SavingChanges the operation has already run its
    // pre-commit handlers, whose cross-module writes hold row locks the competing change would wait on until
    // the command timeout — a deadlock no scheduler could produce, because the operation is itself blocked on
    // the competing change.
    public async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (competingChange is { } pending &&
            losingEntityType is { } entityType &&
            eventData.Context is { } context &&
            Reads(command, context, entityType))
        {
            competingChange = null;
            await RunDetachedAsync(pending);
            committed = true;
        }

        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (committed && HasPendingUpdate(eventData.Context!, losingEntityType!))
        {
            committed = false;
            ForcedConflicts++;
        }

        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// The competing change stands for an independent caller, so it must not inherit the operation's ambient
    /// state. Suppressing execution-context flow detaches both the operation's transaction — whose locks it
    /// would otherwise wait on — and its <c>HttpContext</c>, which would leave the change's own scope neither
    /// host nor tenant-resolved and so make every tenant-filtered read return nothing.
    /// </summary>
    private static Task RunDetachedAsync(Func<Task> competingChange)
    {
        using (ExecutionContext.SuppressFlow())
            return Task.Run(competingChange);
    }

    /// <summary>
    /// The read an update can be built from is the one that fetches the row's concurrency tokens, so a
    /// projection of a key or a single column -- which the same flow may issue first to resolve the row --
    /// does not open the window.
    /// </summary>
    private static bool Reads(DbCommand command, DbContext context, Type entityType)
    {
        if (context.Model.FindEntityType(entityType) is not { } type ||
            type.GetTableName() is not { } table ||
            !command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
            !command.CommandText.Contains($"[{table}]", StringComparison.Ordinal))
            return false;

        var tokens = type.GetProperties()
            .Where(property => property.IsConcurrencyToken)
            .Select(property => property.GetColumnName())
            .ToArray();

        return tokens.Length > 0 &&
            tokens.All(column => command.CommandText.Contains($"[{column}]", StringComparison.Ordinal));
    }

    private static bool HasPendingUpdate(DbContext context, Type entityType) =>
        context.ChangeTracker.Entries().Any(entry =>
            entry.State == EntityState.Modified && entityType.IsInstanceOfType(entry.Entity));
}
