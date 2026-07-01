namespace Concertable.Kernel.Identity;

/// <summary>
/// Resolves the current request's tenant once, up front, so the synchronous <see cref="ITenantContext"/>
/// getters (read by EF query filters at translation time) return the memoized value. Request middleware
/// calls this after authentication; the resolution is a no-op for host/anonymous callers.
/// </summary>
public interface ITenantResolver
{
    Task ResolveAsync(CancellationToken ct = default);
}
