using Concertable.Messaging.AspNetCore.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.AspNetCore.UnitTests;

public sealed class AddPausableRequestsTests
{
    [Fact]
    public void AddPausableRequests_ExposesTheSameInstanceAsIPausable()
    {
        var provider = new ServiceCollection()
            .AddPausableRequests()
            .BuildServiceProvider();

        var pausable = provider.GetRequiredService<IPausable>();
        var requests = provider.GetRequiredService<PausableRequests>();

        Assert.Same(requests, pausable);
    }

    [Fact]
    public void AddPausableRequests_ConfiguresExemptPathPrefixes()
    {
        var provider = new ServiceCollection()
            .AddPausableRequests(options => options.ExemptPathPrefixes.Add("/health"))
            .BuildServiceProvider();

        var options = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<PausableRequestsOptions>>().Value;

        Assert.Contains("/health", options.ExemptPathPrefixes);
    }
}
