namespace Concertable.Messaging.AzureServiceBus;

public sealed class AzureServiceBusOptions
{
    public string ConnectionString { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string EventTopicPrefix { get; set; } = "event-";
    public string CommandQueuePrefix { get; set; } = "command-";

    public string TopicNameFor(Type eventType) =>
        EventTopicPrefix + eventType.Name.ToLowerInvariant();

    public string QueueNameFor(Type commandType) =>
        CommandQueuePrefix + commandType.Name.ToLowerInvariant();
}
