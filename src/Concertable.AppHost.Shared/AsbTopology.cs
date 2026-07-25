using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;
using Concertable.Messaging.AzureServiceBus.Options;

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
}
