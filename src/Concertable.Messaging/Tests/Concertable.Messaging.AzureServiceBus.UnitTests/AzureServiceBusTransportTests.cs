using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AzureServiceBus.UnitTests;

public class AzureServiceBusTransportTests
{
    private readonly AzureServiceBusOptions options = new()
    {
        ConnectionString = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=x;SharedAccessKey=y",
        ServiceName = "b2b",
    };
    private readonly MessageSerializer serializer = new();

    private AzureServiceBusTransport CreateSut()
    {
        // ServiceBusClient ctor accepts a fake connection string without opening a network connection.
        var client = new ServiceBusClient(options.ConnectionString);
        return new AzureServiceBusTransport(client, Options.Create(options), serializer);
    }

    [Fact]
    public void BuildMessage_PopulatesMessageIdContentTypeAndApplicationProperties()
    {
        // Arrange
        var sut = CreateSut();
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1);
        var envelope = new MessageEnvelope(
            MessageId: Guid.NewGuid(),
            MessageType: typeof(FakeIntegrationEvent).FullName!,
            OccurredAtUtc: new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero),
            CorrelationId: "corr-123");

        // Act
        var message = sut.BuildMessage(payload, envelope);

        // Assert
        Assert.Equal(envelope.MessageId.ToString(), message.MessageId);
        Assert.Equal("application/json", message.ContentType);
        Assert.Equal(envelope.MessageType, message.ApplicationProperties["MessageType"]);
        Assert.Equal(envelope.OccurredAtUtc.ToString("O"), message.ApplicationProperties["OccurredAtUtc"]);
        Assert.Equal("corr-123", message.CorrelationId);
    }

    [Fact]
    public void BuildMessage_WhenCorrelationIdIsNull_DoesNotSetCorrelationIdOnServiceBusMessage()
    {
        // Arrange
        var sut = CreateSut();
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 1);
        var envelope = new MessageEnvelope(
            MessageId: Guid.NewGuid(),
            MessageType: typeof(FakeIntegrationEvent).FullName!,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            CorrelationId: null);

        // Act
        var message = sut.BuildMessage(payload, envelope);

        // Assert
        Assert.Null(message.CorrelationId);
    }

    [Fact]
    public void BuildMessage_BodyIsJsonRoundTrippableToOriginalPayload()
    {
        // Arrange
        var sut = CreateSut();
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 7);
        var envelope = new MessageEnvelope(
            MessageId: Guid.NewGuid(),
            MessageType: typeof(FakeIntegrationEvent).FullName!,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var message = sut.BuildMessage(payload, envelope);
        var roundTripped = (FakeIntegrationEvent)serializer.Deserialize(message.Body, typeof(FakeIntegrationEvent));

        // Assert
        Assert.Equal(payload, roundTripped);
    }

    // PublishAsync/SendAsync are not exercised here because ServiceBusSender is sealed with no
    // mockable surface in Azure.Messaging.ServiceBus 7.18.x — the SDK provides no test seams for
    // sender behaviour. Coverage of message construction is via BuildMessage above; broker round-trips
    // are integration-level concerns out of scope for this unit test project.
}
