namespace Concertable.Messaging.UnitTests;

public class MessageTypeRegistryTests
{
    [Fact]
    public void RegisterEvent_AfterRegistration_ResolvesByFullName()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterEvent<FakeIntegrationEvent>();
        var resolved = registry.ResolveEvent(typeof(FakeIntegrationEvent).FullName!);

        // Assert
        Assert.Equal(typeof(FakeIntegrationEvent), resolved);
    }

    [Fact]
    public void RegisterCommand_AfterRegistration_ResolvesByFullName()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterCommand<FakeIntegrationCommand>();
        var resolved = registry.ResolveCommand(typeof(FakeIntegrationCommand).FullName!);

        // Assert
        Assert.Equal(typeof(FakeIntegrationCommand), resolved);
    }

    [Fact]
    public void RegisteredEventTypes_AfterMultipleRegistrations_ContainsAllEvents()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterEvent<FakeIntegrationEvent>();
        registry.RegisterEvent<OtherFakeEvent>();

        // Assert
        Assert.Contains(typeof(FakeIntegrationEvent), registry.RegisteredEventTypes);
        Assert.Contains(typeof(OtherFakeEvent), registry.RegisteredEventTypes);
        Assert.Equal(2, registry.RegisteredEventTypes.Count());
    }

    [Fact]
    public void RegisteredCommandTypes_AfterCommandRegistration_DoesNotContainEvents()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterEvent<FakeIntegrationEvent>();
        registry.RegisterCommand<FakeIntegrationCommand>();

        // Assert
        Assert.DoesNotContain(typeof(FakeIntegrationEvent), registry.RegisteredCommandTypes);
        Assert.Contains(typeof(FakeIntegrationCommand), registry.RegisteredCommandTypes);
    }

    [Fact]
    public void ResolveEvent_WhenUnregistered_ThrowsKeyNotFound()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act + Assert
        Assert.Throws<KeyNotFoundException>(() => registry.ResolveEvent("Some.Unknown.Type"));
    }

    [Fact]
    public void RegisterEvent_WhenCalledTwice_LastWriteWins()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterEvent<FakeIntegrationEvent>();
        registry.RegisterEvent<FakeIntegrationEvent>();

        // Assert
        Assert.Single(registry.RegisteredEventTypes);
    }
}
