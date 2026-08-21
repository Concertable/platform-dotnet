using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AzureServiceBus.UnitTests;

public sealed class AzureServiceBusTransportTests
{
    private readonly AzureServiceBusOptions options;
    private readonly MessageSerializer serializer;
    private readonly AzureServiceBusTransport transport;
    private readonly AzureServiceBusTransport transportWithoutDestination;

    public AzureServiceBusTransportTests()
    {
        this.options = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=x;SharedAccessKey=y",
            ServiceName = "b2b",
        };
        this.serializer = new MessageSerializer();
        var registry = new MessageTypeRegistry();
        registry.RegisterCommandSender<FakeIntegrationCommand>("payment");
        var client = new ServiceBusClient(this.options.ConnectionString);
        this.transport = new AzureServiceBusTransport(
            client,
            Microsoft.Extensions.Options.Options.Create(this.options),
            this.serializer,
            registry);
        this.transportWithoutDestination = new AzureServiceBusTransport(
            client,
            Microsoft.Extensions.Options.Options.Create(this.options),
            this.serializer,
            new MessageTypeRegistry());
    }

    [Fact]
    public void QueueNameForCommand_RegisteredDestination_UsesDestinationService()
    {
        var queue = this.transport.QueueNameForCommand(typeof(FakeIntegrationCommand));

        Assert.Equal("command-payment-fakeintegrationcommand", queue);
    }

    [Fact]
    public void QueueNameForCommand_NoRegisteredDestination_UsesCurrentService()
    {
        var queue = this.transportWithoutDestination.QueueNameForCommand(typeof(FakeIntegrationCommand));

        Assert.Equal("command-b2b-fakeintegrationcommand", queue);
    }

    [Fact]
    public void BuildMessage_PopulatesMessageIdContentTypeAndApplicationProperties()
    {
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1);
        var envelope = new MessageEnvelope(
            MessageId: Guid.NewGuid(),
            MessageType: typeof(FakeIntegrationEvent).FullName!,
            OccurredAtUtc: new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero),
            CorrelationId: "corr-123");

        var message = this.transport.BuildMessage(payload, envelope);

        Assert.Equal(envelope.MessageId.ToString(), message.MessageId);
        Assert.Equal("application/json", message.ContentType);
        Assert.Equal(envelope.MessageType, message.ApplicationProperties["MessageType"]);
        Assert.Equal(envelope.OccurredAtUtc.ToString("O"), message.ApplicationProperties["OccurredAtUtc"]);
        Assert.Equal("corr-123", message.CorrelationId);
    }

    [Fact]
    public void BuildMessage_WhenCorrelationIdIsNull_DoesNotSetCorrelationIdOnServiceBusMessage()
    {
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1);
        var envelope = new MessageEnvelope(
            MessageId: Guid.NewGuid(),
            MessageType: typeof(FakeIntegrationEvent).FullName!,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            CorrelationId: null);

        var message = this.transport.BuildMessage(payload, envelope);

        Assert.Null(message.CorrelationId);
    }

    [Fact]
    public void BuildMessage_BodyIsJsonRoundTrippableToOriginalPayload()
    {
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 7);
        var envelope = new MessageEnvelope(
            MessageId: Guid.NewGuid(),
            MessageType: typeof(FakeIntegrationEvent).FullName!,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        var message = this.transport.BuildMessage(payload, envelope);
        var roundTripped = (FakeIntegrationEvent)this.serializer.Deserialize(
            message.Body,
            typeof(FakeIntegrationEvent));

        Assert.Equal(payload, roundTripped);
    }

    // PublishAsync/SendAsync are not exercised here because ServiceBusSender is sealed with no
    // mockable surface in Azure.Messaging.ServiceBus 7.18.x — the SDK provides no test seams for
    // sender behaviour. Coverage of message construction is via BuildMessage above; broker round-trips
    // are integration-level concerns out of scope for this unit test project.
}
