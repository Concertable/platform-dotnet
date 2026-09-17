using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Extensions;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.UnitTests;

public sealed class OutboxServiceCollectionExtensionsTests
{
    private readonly ServiceCollection services = new();

    #region AddOutbox

    [Fact]
    public void AddOutbox_NothingConfigured_KeepsTheDefaults()
    {
        services.AddOutbox(options => options.UseInMemoryDatabase("outbox"), runDispatcher: false);

        var configured = Resolve();
        Assert.Equal(Schema.Name, configured.SchemaName);
        Assert.Equal(TimeSpan.FromMinutes(5), configured.LeaseDuration);
    }

    [Fact]
    public void AddOutbox_AConfigureCallback_AppliesEverythingItSets()
    {
        services.AddOutbox(
            options => options.UseInMemoryDatabase("outbox"),
            outbox =>
            {
                outbox.SchemaName = "tickets";
                outbox.BatchSize = 7;
                outbox.LeaseDuration = TimeSpan.FromSeconds(30);
            },
            runDispatcher: false);

        var configured = Resolve();
        Assert.Equal("tickets", configured.SchemaName);
        Assert.Equal(7, configured.BatchSize);
        Assert.Equal(TimeSpan.FromSeconds(30), configured.LeaseDuration);
    }

    [Fact]
    public void AddOutbox_TheProviderResolvingOverload_AppliesTheSameConfiguration()
    {
        services.AddOutbox(
            (_, options) => options.UseInMemoryDatabase("outbox"),
            outbox => outbox.SchemaName = "tickets",
            runDispatcher: false);

        Assert.Equal("tickets", Resolve().SchemaName);
    }

    [Fact]
    public void AddOutbox_AnyOverload_ReturnsTheCollectionItWasGiven() =>
        Assert.Same(services, services.AddOutbox(options => options.UseInMemoryDatabase("outbox"), runDispatcher: false));

    #endregion

    private OutboxOptions Resolve()
    {
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
    }
}
