namespace Concertable.Kernel;

/// <summary>
/// Carries the id of the tenant a row belongs to — the read capability at the base of the tenant-entity
/// family. <see cref="ITenantScoped"/> extends it with tenant policy (interceptor-stamped, reads filtered,
/// cross-tenant writes blocked); a row keyed by the tenant itself (one per tenant, e.g. a counter)
/// implements this directly. The value is infrastructure-assigned, never set by domain logic, so the
/// contract is read-only.
/// </summary>
public interface ITenant
{
    Guid TenantId { get; }
}
