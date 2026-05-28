using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Concertable.Messaging.Application;
using Concertable.Messaging.AzureServiceBus.Options;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AzureServiceBus;

internal sealed class AzureServiceBusTransport : IBusTransport, IAsyncDisposable
{
    private readonly ServiceBusClient client;
    private readonly AzureServiceBusOptions options;
    private readonly MessageSerializer serializer;
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders = new();

    public AzureServiceBusTransport(
        ServiceBusClient client,
        IOptions<AzureServiceBusOptions> options,
        MessageSerializer serializer)
    {
        this.client = client;
        this.options = options.Value;
        this.serializer = serializer;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        var topic = options.TopicNameFor(typeof(TEvent));
        var sender = senders.GetOrAdd(topic, name => client.CreateSender(name));
        try
        {
            await sender.SendMessageAsync(BuildMessage(@event, envelope), ct);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
        }
    }

    public async Task SendAsync<TCommand>(TCommand command, MessageEnvelope envelope, CancellationToken ct = default)
        where TCommand : IIntegrationCommand
    {
        var queue = options.QueueNameFor(typeof(TCommand));
        var sender = senders.GetOrAdd(queue, name => client.CreateSender(name));
        await sender.SendMessageAsync(BuildMessage(command, envelope), ct);
    }

    internal ServiceBusMessage BuildMessage<T>(T payload, MessageEnvelope envelope)
    {
        var msg = new ServiceBusMessage(serializer.Serialize(payload))
        {
            MessageId = envelope.MessageId.ToString(),
            ContentType = "application/json",
        };
        msg.ApplicationProperties["MessageType"] = envelope.MessageType;
        msg.ApplicationProperties["OccurredAtUtc"] = envelope.OccurredAtUtc.ToString("O");
        if (envelope.CorrelationId is not null)
            msg.CorrelationId = envelope.CorrelationId;
        return msg;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in senders.Values)
            await sender.DisposeAsync();
        await client.DisposeAsync();
    }
}
