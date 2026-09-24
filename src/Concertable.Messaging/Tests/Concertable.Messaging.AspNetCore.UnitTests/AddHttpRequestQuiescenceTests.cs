using Concertable.Messaging.AspNetCore;
using Concertable.Messaging.AspNetCore.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Messaging.AspNetCore.UnitTests;

public sealed class AddHttpRequestQuiescenceTests
{
    [Fact]
    public void AddHttpRequestQuiescence_ExposesTheHttpParticipantAsTheSameIngressQuiescerInstance()
    {
        var provider = new ServiceCollection()
            .AddHttpRequestQuiescence()
            .BuildServiceProvider();

        var participant = provider.GetRequiredService<IIngressQuiescer>();
        var http = provider.GetRequiredService<HttpIngressQuiescer>();

        Assert.Same(http, participant);
    }

    [Fact]
    public void AddHttpRequestQuiescence_ConfiguresExemptPathPrefixes()
    {
        var provider = new ServiceCollection()
            .AddHttpRequestQuiescence(options => options.ExemptPathPrefixes.Add("/health"))
            .BuildServiceProvider();

        var options = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HttpQuiescenceOptions>>().Value;

        Assert.Contains("/health", options.ExemptPathPrefixes);
    }
}
