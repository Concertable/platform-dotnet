using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Concertable.Messaging.AzureServiceBus.Options;

namespace Concertable.AppHost.Shared.UnitTests;

public sealed class ServiceTopologyTests
{
    private sealed record SampleFirstEvent;

    private sealed record SampleSecondEvent;

    private sealed record SampleThirdEvent;

    private sealed record SampleCommand;

    [Fact]
    public void PublishAndSubscribe_ProvisionOneTopic()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .Publish<SampleFirstEvent>()
            .WithService("consumer")
            .Subscribe<SampleFirstEvent>()
            .RunAsEmulator();

        var topicName = new AzureServiceBusOptions().TopicNameFor(typeof(SampleFirstEvent));
        var topics = builder.Resources
            .OfType<AzureServiceBusTopicResource>()
            .Where(resource => resource.Name == topicName);
        var subscription = Assert.Single(builder.Resources.OfType<AzureServiceBusSubscriptionResource>());

        Assert.Single(topics);
        Assert.Equal("consumer", subscription.SubscriptionName);
    }

    [Fact]
    public void WithService_PerService_ScopesEachSubscriptionToItsOwnServiceName()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .Publish<SampleFirstEvent>()
            .Publish<SampleSecondEvent>()
            .WithService("service-a")
            .Subscribe<SampleFirstEvent>()
            .WithService("service-b")
            .Subscribe<SampleSecondEvent>()
            .RunAsEmulator();

        var subscriptionNames = builder.Resources
            .OfType<AzureServiceBusSubscriptionResource>()
            .Select(subscription => subscription.SubscriptionName)
            .ToHashSet();

        Assert.Equal(["service-a", "service-b"], subscriptionNames.Order());
    }

    [Fact]
    public void WithService_ScopedBuildersStayIndependentWhenInterleaved()
    {
        var builder = DistributedApplication.CreateBuilder();
        var topology = builder.AddAzureServiceBus("messaging").Topology();
        var serviceA = topology.WithService("service-a");
        var serviceB = topology.WithService("service-b");

        serviceA.Subscribe<SampleFirstEvent>();
        serviceB.Subscribe<SampleSecondEvent>();
        serviceA.Subscribe<SampleThirdEvent>();

        var perService = builder.Resources
            .OfType<AzureServiceBusSubscriptionResource>()
            .GroupBy(subscription => subscription.SubscriptionName)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(2, perService["service-a"]);
        Assert.Equal(1, perService["service-b"]);
    }

    [Fact]
    public void WithService_Publish_ProvisionsTheTopicWithoutScopingIt()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .WithService("service-a")
            .Publish<SampleFirstEvent>()
            .Subscribe<SampleSecondEvent>();

        var topicName = new AzureServiceBusOptions().TopicNameFor(typeof(SampleFirstEvent));
        var topics = builder.Resources.OfType<AzureServiceBusTopicResource>().Select(topic => topic.Name);
        var subscriptions = builder.Resources
            .OfType<AzureServiceBusSubscriptionResource>()
            .Select(subscription => subscription.SubscriptionName);

        Assert.Contains(topicName, topics);
        Assert.Equal(["service-a"], subscriptions);
    }

    [Fact]
    public void WithService_Queue_NamesTheQueueForThatService()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .WithService("service-a")
            .Queue<SampleCommand>();

        var expected = new AzureServiceBusOptions().QueueNameFor("service-a", typeof(SampleCommand));
        var queues = builder.Resources.OfType<AzureServiceBusQueueResource>().Select(queue => queue.QueueName);

        Assert.Contains(expected, queues);
    }

    [Fact]
    public void PublishWithoutSubscriber_ProvisionsExpiringEmulatorSink()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddAzureServiceBus("messaging")
            .Topology()
            .Publish<SampleFirstEvent>()
            .RunAsEmulator();

        var subscription = Assert.Single(builder.Resources.OfType<AzureServiceBusSubscriptionResource>());

        Assert.Equal("emulator-sink", subscription.SubscriptionName);
        Assert.Equal(TimeSpan.FromMinutes(1), subscription.DefaultMessageTimeToLive);
    }
}
