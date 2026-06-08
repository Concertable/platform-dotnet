using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.UnitTests;

public sealed class MessageTypeRegistryTests
{
    [Fact]
    public void RegisterEvent_AfterRegistration_ResolvesByCanonicalName()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterEvent<FakeIntegrationEvent>();
        var resolved = registry.ResolveEvent(MessageTypeAttribute.Resolve(typeof(FakeIntegrationEvent)));

        // Assert
        Assert.Equal(typeof(FakeIntegrationEvent), resolved);
    }

    [Fact]
    public void RegisterCommand_AfterRegistration_ResolvesByCanonicalName()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterCommand<FakeIntegrationCommand>();
        var resolved = registry.ResolveCommand(MessageTypeAttribute.Resolve(typeof(FakeIntegrationCommand)));

        // Assert
        Assert.Equal(typeof(FakeIntegrationCommand), resolved);
    }

    [Fact]
    public void RegisterSubscription_AfterMultipleRegistrations_ContainsAllSubscribedEvents()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterSubscription<FakeIntegrationEvent>();
        registry.RegisterSubscription<OtherFakeEvent>();

        // Assert
        Assert.Contains(typeof(FakeIntegrationEvent), registry.SubscribedEventTypes);
        Assert.Contains(typeof(OtherFakeEvent), registry.SubscribedEventTypes);
        Assert.Equal(2, registry.SubscribedEventTypes.Count());
    }

    [Fact]
    public void RegisterEvent_MakesEventResolvableWithoutMarkingItSubscribed()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterEvent<FakeIntegrationEvent>();

        // Assert
        Assert.Equal(typeof(FakeIntegrationEvent), registry.ResolveEvent(MessageTypeAttribute.Resolve(typeof(FakeIntegrationEvent))));
        Assert.Empty(registry.SubscribedEventTypes);
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
    public void RegisterSubscription_WhenCalledTwice_DeduplicatesSubscribedEvent()
    {
        // Arrange
        var registry = new MessageTypeRegistry();

        // Act
        registry.RegisterSubscription<FakeIntegrationEvent>();
        registry.RegisterSubscription<FakeIntegrationEvent>();

        // Assert
        Assert.Single(registry.SubscribedEventTypes);
    }
}
