namespace Concertable.Kernel;

public interface ITenantScoped
{
    /// <summary>
    /// The owning tenant. Settable so <c>TenantInterceptor</c> can stamp it at SaveChanges
    /// (mirroring <see cref="IAuditable"/>); domain code never sets it directly.
    /// </summary>
    Guid TenantId { get; set; }
}
