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

    public AsbTopology(IResourceBuilder<AzureServiceBusResource> asb) => this.asb = asb;

    public AsbTopology Publish<TEvent>()
    {
        GetOrAddTopic<TEvent>();
        return this;
    }

    public AsbTopology Subscribe<TEvent>(string serviceName)
    {
        var topicBuilder = GetOrAddTopic<TEvent>();
        topicBuilder.AddServiceBusSubscription($"{serviceName}-{KebabCase(typeof(TEvent))}", serviceName);
        return this;
    }

    public AsbTopology Queue<TCommand>(string serviceName)
    {
        asb.AddServiceBusQueue(options.QueueNameFor(serviceName, typeof(TCommand)));
        return this;
    }

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
