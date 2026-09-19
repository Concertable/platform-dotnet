namespace Concertable.Kernel.Identity;

public interface ITenantContext
{
    Guid? TenantId { get; }

    bool HasTenant => TenantId.HasValue;
}
