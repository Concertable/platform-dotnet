using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.ServiceDefaults;

/// <summary>
/// The shared, web-only rate-limiting seam. Kept separate from <c>AddServiceDefaults</c> because
/// non-web hosts (Workers, Seed simulators) also call that and have no HTTP pipeline. Web hosts opt in
/// with <see cref="AddDefaultRateLimiting"/> + <see cref="UseDefaultRateLimiting"/>; each service then
/// applies the named <see cref="RateLimitPolicies"/> to the endpoints it owns.
/// </summary>
public static class RateLimitingExtensions
{
    public static IHostApplicationBuilder AddDefaultRateLimiting(this IHostApplicationBuilder builder)
    {
        var options = new RateLimitingOptions();
        builder.Configuration.GetSection(RateLimitingOptions.SectionName).Bind(options);

        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => CreatePartition(context, options.Global, preferUser: true));

            limiter.AddPolicy(RateLimitPolicies.Login, context => CreatePartition(context, options.Login, preferUser: false));
            limiter.AddPolicy(RateLimitPolicies.Apply, context => CreatePartition(context, options.Apply, preferUser: true));
            limiter.AddPolicy(RateLimitPolicies.Messaging, context => CreatePartition(context, options.Messaging, preferUser: true));
            limiter.AddPolicy(RateLimitPolicies.Upload, context => CreatePartition(context, options.Upload, preferUser: false));

            limiter.OnRejected = OnRejectedAsync;
        });

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

    private static RateLimitPartition<string> CreatePartition(HttpContext context, RateLimitWindow window, bool preferUser) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ResolvePartitionKey(context, preferUser),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = window.PermitLimit,
                Window = TimeSpan.FromSeconds(window.WindowSeconds),
                QueueLimit = window.QueueLimit
            });

    private static string ResolvePartitionKey(HttpContext context, bool preferUser)
    {
        if (preferUser && context.User.Identity?.IsAuthenticated == true)
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
