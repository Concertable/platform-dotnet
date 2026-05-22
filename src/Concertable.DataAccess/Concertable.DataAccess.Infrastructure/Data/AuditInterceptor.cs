using Concertable.Shared.Infrastructure.Identity;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Concertable.DataAccess.Infrastructure;

public class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var entries = eventData.Context!.ChangeTracker.Entries<IAuditable>();
        var now = DateTime.UtcNow;
        var userId = currentUser.Id?.ToString() ?? string.Empty;

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

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
