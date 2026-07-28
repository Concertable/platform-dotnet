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

    public AsbTopology Subscribe<TEvent>(string subscription, string consumerGroup)
    {
        var topic = options.TopicNameFor(typeof(TEvent));
        if (!topics.TryGetValue(topic, out var topicBuilder))
            topics[topic] = topicBuilder = asb.AddServiceBusTopic(topic);

        topicBuilder.AddServiceBusSubscription(subscription, consumerGroup);
        return this;
    }

    public AsbTopology Queue(string queue)
    {
        asb.AddServiceBusQueue(queue);
        return this;
    }

    public AsbTopology Subscribe<TEvent>(string consumerGroup) =>
        Subscribe<TEvent>($"{consumerGroup}-{KebabCase(typeof(TEvent))}", consumerGroup);

    public AsbTopology Queue<TCommand>(string serviceName) =>
        Queue(options.QueueNameFor(serviceName, typeof(TCommand)));

    private static string KebabCase(Type eventType)
    {
        var name = eventType.Name.EndsWith(EventSuffix, StringComparison.Ordinal)
            ? eventType.Name[..^EventSuffix.Length]
            : eventType.Name;
        return Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
    }
}
