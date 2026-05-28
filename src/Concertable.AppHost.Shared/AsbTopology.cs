using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.ServiceBus;

public sealed class AsbTopology
{
    private readonly IResourceBuilder<AzureServiceBusResource> asb;
    private readonly Dictionary<string, IResourceBuilder<AzureServiceBusTopicResource>> topics = new();

    public AsbTopology(IResourceBuilder<AzureServiceBusResource> asb) => this.asb = asb;

    public AsbTopology Subscribe(string topic, string subscription, string consumerGroup)
    {
        if (!topics.TryGetValue(topic, out var topicBuilder))
            topics[topic] = topicBuilder = asb.AddServiceBusTopic(topic);

        topicBuilder.AddServiceBusSubscription(subscription, consumerGroup);
        return this;
    }
}
