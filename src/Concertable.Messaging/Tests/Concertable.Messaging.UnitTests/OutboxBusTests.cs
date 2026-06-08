using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.Messaging.UnitTests;

public sealed class OutboxBusTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    private static (OutboxBus bus, Mock<IOutboxWriter> writer) CreateSut()
    {
        var writer = new Mock<IOutboxWriter>();
        var bus = new OutboxBus(writer.Object, new MessageSerializer(), new FakeTimeProvider(Now));
        return (bus, writer);
    }

    [Fact]
    public async Task PublishAsync_EnqueuesOutboxRowWithEventKindAndSerializedPayload()
    {
        // Arrange
        var (bus, writer) = CreateSut();
        var @event = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 7);
        OutboxMessageEntity? captured = null;
        writer.Setup(w => w.AddAsync(It.IsAny<OutboxMessageEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessageEntity, CancellationToken>((row, _) => captured = row)
            .Returns(Task.CompletedTask);

        // Act
        await bus.PublishAsync(@event);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(MessageKind.Event, captured!.Kind);
        Assert.Equal(MessageTypeAttribute.Resolve(typeof(FakeIntegrationEvent)), captured.MessageType);
        Assert.Equal(Now, captured.OccurredAtUtc);
        Assert.Equal(OutboxStatus.Pending, captured.Status);
        Assert.Contains("\"name\":\"concert\"", captured.Payload);
        Assert.Contains("\"count\":7", captured.Payload);
    }

    [Fact]
    public async Task SendAsync_EnqueuesOutboxRowWithCommandKindAndSerializedPayload()
    {
        // Arrange
        var (bus, writer) = CreateSut();
        var command = new FakeIntegrationCommand(Guid.NewGuid(), "refund");
        OutboxMessageEntity? captured = null;
        writer.Setup(w => w.AddAsync(It.IsAny<OutboxMessageEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessageEntity, CancellationToken>((row, _) => captured = row)
            .Returns(Task.CompletedTask);

        // Act
        await bus.SendAsync(command);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(MessageKind.Command, captured!.Kind);
        Assert.Equal(MessageTypeAttribute.Resolve(typeof(FakeIntegrationCommand)), captured.MessageType);
        Assert.Contains("\"reason\":\"refund\"", captured.Payload);
    }
}
