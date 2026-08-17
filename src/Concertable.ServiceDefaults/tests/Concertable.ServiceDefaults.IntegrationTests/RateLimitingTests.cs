using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Concertable.ServiceDefaults.IntegrationTests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task Apply_OverLimit_Returns429WithRetryAfter()
    {
        const int permitLimit = 3;
        await using var app = await StartAppAsync(permitLimit);
        var client = app.GetTestClient();

        for (var i = 0; i < permitLimit; i++)
        {
            using var allowed = await client.GetAsync("/apply");
            allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        using var rejected = await client.GetAsync("/apply");

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
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
