namespace Concertable.Messaging.UnitTests;

public sealed class MessageSerializerTests
{
    private readonly MessageSerializer sut = new();

    [Fact]
    public void Serialize_ProducesNonEmptyBinaryData()
    {
        // Arrange
        var payload = new FakeIntegrationEvent(Guid.NewGuid(), "name", 3);

        // Act
        var data = sut.Serialize(payload);

        // Assert
        Assert.NotNull(data);
        Assert.NotEmpty(data.ToArray());
    }

    [Fact]
    public void Deserialize_RoundTripsFakeIntegrationEvent_PreservesAllFields()
    {
        // Arrange
        var original = new FakeIntegrationEvent(Guid.NewGuid(), "concert", 42);
        var data = sut.Serialize(original);

        // Act
        var roundTripped = (FakeIntegrationEvent)sut.Deserialize(data, typeof(FakeIntegrationEvent));

        // Assert
        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.Name, roundTripped.Name);
        Assert.Equal(original.Count, roundTripped.Count);
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void Deserialize_RoundTripsFakeIntegrationCommand_PreservesAllFields()
    {
        // Arrange
        var original = new FakeIntegrationCommand(Guid.NewGuid(), "refund");
        var data = sut.Serialize(original);

        // Act
        var roundTripped = (FakeIntegrationCommand)sut.Deserialize(data, typeof(FakeIntegrationCommand));

        // Assert
        Assert.Equal(original, roundTripped);
    }
}
