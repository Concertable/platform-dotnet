using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Concertable.ServiceDefaults;

/// <summary>
/// The shared, web-only rate-limiting seam. Kept separate from <c>AddServiceDefaults</c> because
/// non-web hosts (Workers, Seed simulators) also call that and have no HTTP pipeline. A web host opts in
/// with <see cref="AddDefaultRateLimiting"/> + <see cref="UseDefaultRateLimiting"/>, then declares each
/// abuse surface it owns with <see cref="AddRateLimitPolicy"/>. There is deliberately no global limiter:
/// only the handful of endpoints that are genuine abuse surfaces carry a named policy; everything else is
/// unthrottled, the correct default for authenticated, workflow-bounded domain endpoints.
/// </summary>
public static class RateLimitingExtensions
{
    public static IHostApplicationBuilder AddDefaultRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = OnRejectedAsync;
        });

        return builder;
    }

    /// <summary>
    /// Declares one named fixed-window policy for an abuse surface. The window binds from
    /// <c>RateLimiting:&lt;policyName&gt;</c> over <paramref name="defaults"/>, resolved lazily per request
    /// so a host or test that layers configuration after builder creation still wins. <paramref name="perUser"/>
    /// partitions on the authenticated <c>sub</c> (falling back to IP); pass <see langword="false"/> for an
    /// anonymous surface, which always partitions on client IP.
    /// </summary>
    public static IHostApplicationBuilder AddRateLimitPolicy(
        this IHostApplicationBuilder builder, string policyName, RateLimitWindow defaults, bool perUser)
    {
        builder.Services.AddOptions<RateLimitWindow>(policyName)
            .Configure(window =>
            {
                window.PermitLimit = defaults.PermitLimit;
                window.WindowSeconds = defaults.WindowSeconds;
                window.QueueLimit = defaults.QueueLimit;
            })
            .BindConfiguration($"{RateLimitWindow.ConfigRoot}:{policyName}");

        builder.Services.Configure<RateLimiterOptions>(limiter =>
            limiter.AddPolicy(policyName, context =>
            {
                var window = context.RequestServices
                    .GetRequiredService<IOptionsMonitor<RateLimitWindow>>()
                    .Get(policyName);
                return CreatePartition(context, window, perUser);
            }));

        return builder;
    }

    /// <summary>
    /// Must run after authentication (so <c>sub</c> is populated for per-user partitioning) and routing
    /// (so endpoint-metadata policies resolve), and before the endpoint terminals.
    /// </summary>
    public static WebApplication UseDefaultRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }

    internal static RateLimitPartition<string> CreatePartition(HttpContext context, RateLimitWindow window, bool perUser) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ResolvePartitionKey(context, perUser),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = window.PermitLimit,
                Window = TimeSpan.FromSeconds(window.WindowSeconds),
                QueueLimit = window.QueueLimit
            });

    internal static string ResolvePartitionKey(HttpContext context, bool perUser)
    {
        if (perUser && context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(sub))
                return "user:" + sub;
        }

        return "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        var problemDetails = context.HttpContext.RequestServices.GetService<IProblemDetailsService>();
        if (problemDetails is not null)
            await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails =
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests.",
                    Detail = "Request rate limit exceeded. Retry after the period given by the Retry-After header."
                }
            });
    }
}
