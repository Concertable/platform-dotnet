using Concertable.Messaging.AspNetCore.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AspNetCore.UnitTests;

public sealed class AddGateTests
{
    [Fact]
    public void AddGate_ExposesTheMiddlewareAsTheSameIPausableInstance()
    {
        var provider = new ServiceCollection()
            .AddGate()
            .BuildServiceProvider();

        var pausable = provider.GetRequiredService<IPausable>();
        var gate = provider.GetRequiredService<GateMiddleware>();

        Assert.Same(gate, pausable);
    }

    [Fact]
    public void AddGate_ConfiguresExemptPathPrefixes()
    {
        var provider = new ServiceCollection()
            .AddGate(options => options.ExemptPathPrefixes.Add("/health"))
            .BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<GateOptions>>().Value;

        Assert.Contains("/health", options.ExemptPathPrefixes);
    }
}
