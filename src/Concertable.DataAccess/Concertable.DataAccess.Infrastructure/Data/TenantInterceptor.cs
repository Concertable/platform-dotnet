using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Concertable.Kernel;
using Concertable.Kernel.Identity;

namespace Concertable.DataAccess.Infrastructure.Data;

public sealed class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext tenantContext;

    public TenantInterceptor(ITenantContext tenantContext)
    {
        this.tenantContext = tenantContext;
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

        if (tenantContext.IsHost)
            return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                var current = tenantContext.TenantId
                    ?? throw new InvalidOperationException(
                        "Cannot persist a tenant-scoped entity without a current tenant.");

                if (entry.Entity.TenantId == Guid.Empty)
                    entry.Entity.TenantId = current;
                else if (entry.Entity.TenantId != current)
                    throw new InvalidOperationException(
                        $"Cross-tenant write blocked: entity tenant {entry.Entity.TenantId} does not match current tenant {current}.");
            }
            else if (entry.State == EntityState.Modified
                && tenantContext.TenantId is { } current
                && entry.Entity.TenantId != current)
            {
                throw new InvalidOperationException(
                    $"Cross-tenant modification blocked: entity tenant {entry.Entity.TenantId} does not match current tenant {current}.");
            }
        }
    }
}
