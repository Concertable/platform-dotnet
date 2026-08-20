using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Concertable.ServiceDefaults.UnitTests;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task AddDefaultRateLimiting_OverLimit_RejectsWithRetryAfter()
    {
        const int permitLimit = 3;
        var limiter = BuildGlobalLimiter(permitLimit);
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

    private static PartitionedRateLimiter<HttpContext> BuildGlobalLimiter(int permitLimit)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RateLimitingOptions.SectionName}:Global:PermitLimit"] = permitLimit.ToString(CultureInfo.InvariantCulture),
            [$"{RateLimitingOptions.SectionName}:Global:WindowSeconds"] = "60"
        });
        builder.AddDefaultRateLimiting();

        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
        return options.GlobalLimiter
            ?? throw new InvalidOperationException("AddDefaultRateLimiting did not configure a GlobalLimiter.");
    }
}
