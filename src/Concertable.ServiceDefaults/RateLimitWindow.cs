namespace Concertable.ServiceDefaults;

/// <summary>
/// A fixed-window rate limit: <see cref="PermitLimit"/> requests per <see cref="WindowSeconds"/>. Bound
/// per named policy from the <c>RateLimiting:&lt;PolicyName&gt;</c> configuration section over the
/// launch-sane defaults each service passes to <c>AddRateLimitPolicy</c>; the
/// <c>launch/config-and-deployment</c> gate later tunes those sections from the real config store without
/// a code change.
/// </summary>
public sealed class RateLimitWindow
{
    public const string ConfigRoot = "RateLimiting";

    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }

    /// <summary>Requests to queue past the limit before rejecting. Zero (default) rejects immediately — the right posture for an abuse throttle.</summary>
    public int QueueLimit { get; set; }
}
