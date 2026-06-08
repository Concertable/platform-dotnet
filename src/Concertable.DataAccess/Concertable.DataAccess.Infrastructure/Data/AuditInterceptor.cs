using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.DataAccess.Infrastructure.Data;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private const string SystemActor = "system";

    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;

    public AuditInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var entries = context.ChangeTracker.Entries<IAuditable>();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.Id?.ToString() ?? SystemActor;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedBy = userId;
            }
        }
    }
}
