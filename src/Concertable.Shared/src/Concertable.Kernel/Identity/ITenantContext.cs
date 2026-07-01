namespace Concertable.Kernel.Identity;

/// <summary>Ambient accessor for the current request's tenant. Mirrors <see cref="ICurrentUser"/>.</summary>
public interface ITenantContext
{
    /// <summary>Current tenant; <see langword="null"/> when unresolved (fails closed for non-host callers).</summary>
    Guid? TenantId { get; }

    /// <summary>Guard-clause sugar — never drives the filter bypass.</summary>
    bool HasTenant => TenantId.HasValue;

    /// <summary>System/service caller — the row-level filter bypass.</summary>
    bool IsHost { get; }
}
