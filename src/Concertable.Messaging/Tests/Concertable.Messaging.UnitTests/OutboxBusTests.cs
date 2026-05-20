using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.Messaging.UnitTests;

public class OutboxBusTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    private static (OutboxBus bus, Mock<IOutboxStore> store) CreateSut()
    {
        var store = new Mock<IOutboxStore>();
        var serializer = new MessageSerializer();
        var time = new FakeTimeProvider(Now);
        var bus = new OutboxBus(store.Object, serializer, time);
        return (bus, store);
    }

    [Fact]
    public async Task PublishAsync_EnqueuesOutboxRowWithEventKindAndSerializedPayload()
    {
        // Arrange
        var (bus, store) = CreateSut();
        var @event = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 7);
        OutboxMessageEntity? captured = null;
        store.Setup(s => s.AddAsync(It.IsAny<OutboxMessageEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessageEntity, CancellationToken>((row, _) => captured = row)
            .Returns(Task.CompletedTask);

        // Act
        await bus.PublishAsync(@event);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(MessageKind.Event, captured!.Kind);
        Assert.Equal(typeof(FakeIntegrationEvent).FullName, captured.MessageType);
        Assert.Equal(Now, captured.OccurredAtUtc);
        Assert.Equal(OutboxStatus.Pending, captured.Status);
        Assert.Contains("\"name\":\"concert\"", captured.Payload);
        Assert.Contains("\"count\":7", captured.Payload);
    }

    [Fact]
    public async Task SendAsync_EnqueuesOutboxRowWithCommandKindAndSerializedPayload()
    {
        // Arrange
        var (bus, store) = CreateSut();
        var command = new FakeIntegrationCommand(Guid.NewGuid(), "refund");
        OutboxMessageEntity? captured = null;
        store.Setup(s => s.AddAsync(It.IsAny<OutboxMessageEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessageEntity, CancellationToken>((row, _) => captured = row)
            .Returns(Task.CompletedTask);

        // Act
        await bus.SendAsync(command);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(MessageKind.Command, captured!.Kind);
        Assert.Equal(typeof(FakeIntegrationCommand).FullName, captured.MessageType);
        Assert.Contains("\"reason\":\"refund\"", captured.Payload);
    }

    [Fact]
    public async Task PublishAsync_DoesNotCallSaveChanges()
    {
        // Arrange
        var (bus, store) = CreateSut();

        // Act
        await bus.PublishAsync(new FakeIntegrationEvent(Guid.NewGuid(), "x", 1));

        // Assert
        store.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
