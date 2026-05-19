namespace Concertable.Messaging.AzureServiceBus;

public sealed class AzureServiceBusOptions
{
    public required string ConnectionString { get; init; }
    public required string ServiceName { get; init; }
    public string EventTopicPrefix { get; init; } = "event.";
    public string CommandQueuePrefix { get; init; } = "command.";

    public string TopicNameFor(Type eventType) =>
        EventTopicPrefix + eventType.Name.ToLowerInvariant();

    public string QueueNameFor(Type commandType) =>
        CommandQueuePrefix + commandType.Name.ToLowerInvariant();
}
