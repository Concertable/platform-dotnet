using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Concertable.ServiceDefaults.UnitTests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task Partition_OverLimit_RejectsWithRetryAfter()
    {
        const int permitLimit = 3;
        var window = new RateLimitWindow { PermitLimit = permitLimit, WindowSeconds = 60 };
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitingExtensions.CreatePartition(context, window, perUser: false));
        var context = new DefaultHttpContext();

        for (var i = 0; i < permitLimit; i++)
        {
            using var lease = await limiter.AcquireAsync(context, permitCount: 1);
            Assert.True(lease.IsAcquired);
        }

        using var rejected = await limiter.AcquireAsync(context, permitCount: 1);

        Assert.False(rejected.IsAcquired);
        Assert.True(rejected.TryGetMetadata(MetadataName.RetryAfter, out _));
    }

    [Fact]
    public async Task Partition_PerUser_KeysDistinctUsersSeparately()
    {
        var window = new RateLimitWindow { PermitLimit = 1, WindowSeconds = 60 };
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitingExtensions.CreatePartition(context, window, perUser: true));

        using var first = await limiter.AcquireAsync(AuthenticatedContext("user-a"), permitCount: 1);
        using var second = await limiter.AcquireAsync(AuthenticatedContext("user-b"), permitCount: 1);

        Assert.True(first.IsAcquired);
        Assert.True(second.IsAcquired);
    }

    [Fact]
    public void ResolvePartitionKey_PerUser_UsesSubClaim()
    {
        var key = RateLimitingExtensions.ResolvePartitionKey(AuthenticatedContext("user-a"), perUser: true);

        Assert.Equal("user:user-a", key);
    }

    [Fact]
    public void ResolvePartitionKey_Anonymous_UsesClientIp()
    {
        var key = RateLimitingExtensions.ResolvePartitionKey(AuthenticatedContext("user-a"), perUser: false);

        Assert.StartsWith("ip:", key);
    }

    [Fact]
    public void AddRateLimitPolicy_BindsNamedWindowFromConfigLayeredAfterRegistration_OverDefaults()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddDefaultRateLimiting();
        builder.AddRateLimitPolicy("apply", new RateLimitWindow { PermitLimit = 10, WindowSeconds = 60 }, perUser: true);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:apply:PermitLimit"] = "7"
        });

        using var host = builder.Build();
        var window = host.Services.GetRequiredService<IOptionsMonitor<RateLimitWindow>>().Get("apply");

        Assert.Equal(7, window.PermitLimit);
        Assert.Equal(60, window.WindowSeconds);
    }

    private static DefaultHttpContext AuthenticatedContext(string sub)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", sub)], authenticationType: "test"));
        return context;
    }
}
