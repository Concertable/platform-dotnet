using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Concertable.Messaging.AzureServiceBus.Options;
using System.Text.RegularExpressions;

public sealed class AsbTopology
{
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

    public ServiceScope ForService(string consumerGroup) => new(this, consumerGroup);

    public sealed class ServiceScope
    {
        private readonly AsbTopology topology;
        private readonly string consumerGroup;

        internal ServiceScope(AsbTopology topology, string consumerGroup)
        {
            this.topology = topology;
            this.consumerGroup = consumerGroup;
        }

        public ServiceScope Subscribe<TEvent>()
        {
            topology.Subscribe<TEvent>($"{consumerGroup}-{KebabCase(typeof(TEvent))}", consumerGroup);
            return this;
        }

        public ServiceScope Queue(string queue)
        {
            topology.Queue(queue);
            return this;
        }

        public AsbTopology Topology => topology;

        private static string KebabCase(Type eventType)
        {
            var name = eventType.Name.EndsWith("Event", StringComparison.Ordinal)
                ? eventType.Name[..^"Event".Length]
                : eventType.Name;
            return Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
        }
    }
}
