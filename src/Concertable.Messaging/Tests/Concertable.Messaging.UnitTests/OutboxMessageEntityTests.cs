using Concertable.Kernel;

namespace Concertable.Messaging.UnitTests;

public class OutboxMessageEntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidInputs_ReturnsPendingEntity()
    {
        // Arrange
        var type = typeof(FakeIntegrationEvent);
        var payload = "{\"id\":\"abc\"}";

        // Act
        var entity = OutboxMessageEntity.Create(type, payload, Now, MessageKind.Event, "corr-1");

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(type.FullName, entity.MessageType);
        Assert.Equal(payload, entity.Payload);
        Assert.Equal(Now, entity.OccurredAtUtc);
        Assert.Equal(MessageKind.Event, entity.Kind);
        Assert.Equal("corr-1", entity.CorrelationId);
        Assert.Equal(OutboxStatus.Pending, entity.Status);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Equal(0, entity.Attempts);
        Assert.Null(entity.LastError);
    }

    [Fact]
    public void Create_WithBlankPayload_ThrowsDomainException()
    {
        // Arrange + Act + Assert
        Assert.Throws<DomainException>(() =>
            OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "   ", Now, MessageKind.Event));
    }

    [Fact]
    public void Create_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange + Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
            OutboxMessageEntity.Create(null!, "{}", Now, MessageKind.Event));
    }

    [Fact]
    public void MarkDispatched_OnPending_TransitionsToDispatched()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);
        var dispatchedAt = Now.AddSeconds(5);

        // Act
        entity.MarkDispatched(dispatchedAt);

        // Assert
        Assert.Equal(OutboxStatus.Dispatched, entity.Status);
        Assert.Equal(dispatchedAt, entity.DispatchedAtUtc);
        Assert.Null(entity.LastError);
    }

    [Fact]
    public void MarkDispatched_ClearsExistingLastError()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);
        entity.RecordFailure("boom", maxAttempts: 10);

        // Act
        entity.MarkDispatched(Now.AddSeconds(5));

        // Assert
        Assert.Null(entity.LastError);
    }

    [Fact]
    public void MarkDispatched_OnAlreadyDispatched_IsIdempotent()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);
        var first = Now.AddSeconds(5);
        entity.MarkDispatched(first);

        // Act
        entity.MarkDispatched(Now.AddSeconds(10));

        // Assert
        Assert.Equal(OutboxStatus.Dispatched, entity.Status);
        Assert.Equal(first, entity.DispatchedAtUtc);
    }

    [Fact]
    public void MarkDispatched_OnDeadLettered_ThrowsDomainException()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);
        entity.RecordFailure("boom", maxAttempts: 1);

        // Act + Assert
        Assert.Equal(OutboxStatus.DeadLettered, entity.Status);
        Assert.Throws<DomainException>(() => entity.MarkDispatched(Now));
    }

    [Fact]
    public void RecordFailure_IncrementsAttemptsAndSetsLastError()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);

        // Act
        entity.RecordFailure("transport unavailable", maxAttempts: 5);

        // Assert
        Assert.Equal(1, entity.Attempts);
        Assert.Equal("transport unavailable", entity.LastError);
        Assert.Equal(OutboxStatus.Pending, entity.Status);
    }

    [Fact]
    public void RecordFailure_AtMaxAttempts_TransitionsToDeadLettered()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);
        entity.RecordFailure("err1", maxAttempts: 3);
        entity.RecordFailure("err2", maxAttempts: 3);

        // Act
        entity.RecordFailure("err3", maxAttempts: 3);

        // Assert
        Assert.Equal(3, entity.Attempts);
        Assert.Equal(OutboxStatus.DeadLettered, entity.Status);
        Assert.Equal("err3", entity.LastError);
    }

    [Fact]
    public void RecordFailure_OnDispatched_ThrowsDomainException()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);
        entity.MarkDispatched(Now);

        // Act + Assert
        Assert.Throws<DomainException>(() => entity.RecordFailure("late failure", maxAttempts: 5));
    }

    [Fact]
    public void RecordFailure_WithBlankError_ThrowsDomainException()
    {
        // Arrange
        var entity = OutboxMessageEntity.Create(typeof(FakeIntegrationEvent), "{}", Now, MessageKind.Event);

        // Act + Assert
        Assert.Throws<DomainException>(() => entity.RecordFailure("  ", maxAttempts: 5));
    }
}
