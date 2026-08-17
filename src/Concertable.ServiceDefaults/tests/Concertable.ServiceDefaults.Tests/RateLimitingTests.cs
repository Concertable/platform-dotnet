using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Concertable.ServiceDefaults.Tests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task Apply_policy_rejects_with_429_and_retry_after_once_the_limit_is_exceeded()
    {
        const int permitLimit = 3;
        await using var app = await StartAppAsync(permitLimit);
        var client = app.GetTestClient();

        for (var i = 0; i < permitLimit; i++)
        {
            using var allowed = await client.GetAsync("/apply");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var rejected = await client.GetAsync("/apply");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    private static async Task<WebApplication> StartAppAsync(int permitLimit)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RateLimitingOptions.SectionName}:Apply:PermitLimit"] = permitLimit.ToString(CultureInfo.InvariantCulture),
            [$"{RateLimitingOptions.SectionName}:Apply:WindowSeconds"] = "60"
        });
        builder.AddDefaultRateLimiting();

        var app = builder.Build();
        app.UseDefaultRateLimiting();
        app.MapGet("/apply", () => Results.Ok()).RequireRateLimiting(RateLimitPolicies.Apply);

        await app.StartAsync();
        return app;
    }
}
