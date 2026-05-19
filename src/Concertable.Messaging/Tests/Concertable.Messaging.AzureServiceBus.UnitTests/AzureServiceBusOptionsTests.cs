namespace Concertable.Messaging.AzureServiceBus.UnitTests;

public class AzureServiceBusOptionsTests
{
    [Fact]
    public void TopicNameFor_WithDefaultPrefix_PrependsEventPrefixAndLowercases()
    {
        // Arrange
        var options = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://example",
            ServiceName = "b2b",
        };

        // Act
        var topic = options.TopicNameFor(typeof(FakeIntegrationEvent));

        // Assert
        Assert.Equal("event.fakeintegrationevent", topic);
    }

    [Fact]
    public void QueueNameFor_WithDefaultPrefix_PrependsCommandPrefixAndLowercases()
    {
        // Arrange
        var options = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://example",
            ServiceName = "b2b",
        };

        // Act
        var queue = options.QueueNameFor(typeof(FakeIntegrationCommand));

        // Assert
        Assert.Equal("command.fakeintegrationcommand", queue);
    }

    [Fact]
    public void TopicNameFor_WithCustomPrefix_UsesProvidedPrefix()
    {
        // Arrange
        var options = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://example",
            ServiceName = "customer",
            EventTopicPrefix = "evt-",
        };

        // Act
        var topic = options.TopicNameFor(typeof(OtherFakeEvent));

        // Assert
        Assert.Equal("evt-otherfakeevent", topic);
    }

    [Fact]
    public void QueueNameFor_WithCustomPrefix_UsesProvidedPrefix()
    {
        // Arrange
        var options = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://example",
            ServiceName = "customer",
            CommandQueuePrefix = "cmd-",
        };

        // Act
        var queue = options.QueueNameFor(typeof(FakeIntegrationCommand));

        // Assert
        Assert.Equal("cmd-fakeintegrationcommand", queue);
    }

    [Fact]
    public void TopicNameFor_UsesShortTypeNameOnly_NotFullName()
    {
        // Arrange
        var options = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://example",
            ServiceName = "b2b",
        };

        // Act
        var topic = options.TopicNameFor(typeof(FakeIntegrationEvent));

        // Assert
        Assert.DoesNotContain("concertable", topic);
        Assert.DoesNotContain("unittests", topic);
    }
}
