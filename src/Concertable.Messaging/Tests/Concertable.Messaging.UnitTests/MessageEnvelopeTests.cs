namespace Concertable.Messaging.UnitTests;

public class MessageEnvelopeTests
{
    [Fact]
    public void TypeNameFor_OnConcreteType_ReturnsFullName()
    {
        // Arrange
        var type = typeof(FakeIntegrationEvent);

        // Act
        var name = MessageEnvelope.TypeNameFor(type);

        // Assert
        Assert.Equal(type.FullName, name);
    }

    [Fact]
    public void TypeNameFor_OnNullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => MessageEnvelope.TypeNameFor(type!));
    }

    [Fact]
    public void TypeNameFor_OnOpenGeneric_ThrowsArgumentException()
    {
        // Arrange
        var openGeneric = typeof(List<>).GetGenericArguments()[0];

        // Act + Assert
        Assert.Throws<ArgumentException>(() => MessageEnvelope.TypeNameFor(openGeneric));
    }
}
