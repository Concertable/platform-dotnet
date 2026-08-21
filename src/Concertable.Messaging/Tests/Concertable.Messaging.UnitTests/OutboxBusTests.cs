using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.Messaging.UnitTests;

public sealed class OutboxBusTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<IOutboxWriter> writer;
    private readonly OutboxBus bus;

    public OutboxBusTests()
    {
        this.writer = new Mock<IOutboxWriter>();
        this.bus = new OutboxBus(
            this.writer.Object,
            new MessageSerializer(),
            new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task PublishAsync_EnqueuesOutboxRowWithEventKindAndSerializedPayload()
    {
        var @event = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 7);
        OutboxMessageEntity? captured = null;
        this.writer.Setup(w => w.AddAsync(It.IsAny<OutboxMessageEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessageEntity, CancellationToken>((row, _) => captured = row)
            .Returns(Task.CompletedTask);

        await this.bus.PublishAsync(@event);

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
        var command = new FakeIntegrationCommand(Guid.NewGuid(), "refund");
        OutboxMessageEntity? captured = null;
        this.writer.Setup(w => w.AddAsync(It.IsAny<OutboxMessageEntity>(), It.IsAny<CancellationToken>()))
            .Callback<OutboxMessageEntity, CancellationToken>((row, _) => captured = row)
            .Returns(Task.CompletedTask);

        await this.bus.SendAsync(command);

        Assert.NotNull(captured);
        Assert.Equal(MessageKind.Command, captured!.Kind);
        Assert.Equal(MessageTypeAttribute.Resolve(typeof(FakeIntegrationCommand)), captured.MessageType);
        Assert.Contains("\"reason\":\"refund\"", captured.Payload);
    }
}
