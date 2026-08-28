using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Concertable.Messaging.AzureServiceBus.Options;
using System.Text.RegularExpressions;

public sealed class AsbTopology
{
    private const string EventSuffix = "Event";
    private readonly IResourceBuilder<AzureServiceBusResource> asb;
    private readonly AzureServiceBusOptions options = new();
    private readonly Dictionary<string, IResourceBuilder<AzureServiceBusTopicResource>> topics = new();
    private readonly HashSet<string> subscribedTopics = [];
    private string? serviceName;

    public AsbTopology(IResourceBuilder<AzureServiceBusResource> asb) => this.asb = asb;

    public AsbTopology Publish<TEvent>()
    {
        GetOrAddTopic<TEvent>();
        return this;
    }

    public AsbTopology ForService(string serviceName)
    {
        this.serviceName = serviceName;
        return this;
    }

    public AsbTopology Subscribe<TEvent>()
    {
        var currentServiceName = RequireServiceName();
        var topicBuilder = GetOrAddTopic<TEvent>();
        topicBuilder.AddServiceBusSubscription($"{currentServiceName}-{KebabCase(typeof(TEvent))}", currentServiceName);
        subscribedTopics.Add(topicBuilder.Resource.TopicName);
        return this;
    }

    public AsbTopology Queue<TCommand>()
    {
        var currentServiceName = RequireServiceName();
        asb.AddServiceBusQueue(options.QueueNameFor(currentServiceName, typeof(TCommand)));
        return this;
    }

    public IResourceBuilder<AzureServiceBusResource> RunAsEmulator()
    {
        foreach (var (topic, topicBuilder) in topics)
        {
            if (subscribedTopics.Contains(topic))
                continue;

            topicBuilder
                .AddServiceBusSubscription($"{topic}-emulator-sink", "emulator-sink")
                .WithProperties(subscription => subscription.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1));
        }

        return asb.RunAsEmulator();
    }

    private string RequireServiceName() =>
        serviceName ?? throw new InvalidOperationException($"Call {nameof(ForService)} before Subscribe or Queue.");

    private IResourceBuilder<AzureServiceBusTopicResource> GetOrAddTopic<TEvent>()
    {
        var topic = options.TopicNameFor(typeof(TEvent));
        if (!topics.TryGetValue(topic, out var topicBuilder))
            topics[topic] = topicBuilder = asb.AddServiceBusTopic(topic);

        return topicBuilder;
    }

    private static string KebabCase(Type eventType)
    {
        var name = eventType.Name.EndsWith(EventSuffix, StringComparison.Ordinal)
            ? eventType.Name[..^EventSuffix.Length]
            : eventType.Name;
        return Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
    }
}
