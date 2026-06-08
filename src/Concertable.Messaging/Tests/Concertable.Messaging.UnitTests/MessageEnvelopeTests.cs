using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.UnitTests;

public sealed class MessageTypeAttributeTests
{
    [Fact]
    public void Resolve_OnDecoratedType_ReturnsAttributeName()
    {
        // Arrange
        var type = typeof(FakeIntegrationEvent);

        // Act
        var name = MessageTypeAttribute.Resolve(type);

        // Assert
        Assert.Equal("concertable.messaging.fake-integration-event.v1", name);
    }

    [Fact]
    public void Resolve_OnNullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? type = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() => MessageTypeAttribute.Resolve(type!));
    }

    [Fact]
    public void Resolve_OnTypeMissingAttribute_ThrowsInvalidOperationException()
    {
        // Arrange
        var type = typeof(object);

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() => MessageTypeAttribute.Resolve(type));
    }
}
