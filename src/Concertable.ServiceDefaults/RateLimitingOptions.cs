namespace Concertable.ServiceDefaults;

/// <summary>
/// Rate-limiting limits for the shared web-host seam, bound from the <c>RateLimiting</c> configuration
/// section over the hard-coded defaults here. The defaults are launch-sane; the
/// <c>launch/config-and-deployment</c> gate later binds this type from the real config store without a
/// code change.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Fallback ceiling for any endpoint no named policy covers; keyed on user else IP.</summary>
    public RateLimitWindow Global { get; set; } = new() { PermitLimit = 200, WindowSeconds = 60 };

    public RateLimitWindow Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitWindow Apply { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitWindow Messaging { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };
    public RateLimitWindow Upload { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}

/// <summary>A fixed-window limit: <see cref="PermitLimit"/> requests per <see cref="WindowSeconds"/>.</summary>
public sealed class RateLimitWindow
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }

    /// <summary>Requests to queue past the limit before rejecting. Zero (default) rejects immediately — the right posture for an abuse throttle.</summary>
    public int QueueLimit { get; set; }
}
