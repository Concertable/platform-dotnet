namespace Concertable.Messaging.AzureServiceBus.Options;

public sealed class AzureServiceBusOptions
{
    public const string ServiceNameConfigKey = "ServiceBus:" + nameof(ServiceName);
    public const string ServiceNameEnvVar = "ServiceBus__" + nameof(ServiceName);

    public string ConnectionString { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string EventTopicPrefix { get; set; } = "event-";
    public string CommandQueuePrefix { get; set; } = "command-";

    public string TopicNameFor(Type eventType) =>
        EventTopicPrefix + eventType.Name.ToLowerInvariant();

    public string QueueNameFor(Type commandType) =>
        QueueNameFor(ServiceName, commandType);

    public string QueueNameFor(string serviceName, Type commandType) =>
        $"{CommandQueuePrefix}{serviceName}-{commandType.Name.ToLowerInvariant()}";
}
