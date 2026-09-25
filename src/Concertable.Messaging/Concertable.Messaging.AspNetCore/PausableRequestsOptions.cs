using Microsoft.AspNetCore.Http;

namespace Concertable.Messaging.AspNetCore;

/// <summary>
/// Configures which requests <see cref="PausableRequestsMiddleware"/> never holds. WebSocket upgrades are
/// always exempt; add path prefixes for endpoints that must stay reachable during a pause, such as health
/// probes and the endpoint that drives the reset itself.
/// </summary>
public sealed class PausableRequestsOptions
{
    public IList<string> ExemptPathPrefixes { get; } = new List<string>();

    internal bool IsExempt(PathString path) =>
        ExemptPathPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
