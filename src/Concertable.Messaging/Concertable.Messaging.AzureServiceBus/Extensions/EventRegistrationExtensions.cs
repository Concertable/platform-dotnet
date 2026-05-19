namespace Concertable.Messaging.AzureServiceBus;

public static class EventRegistrationExtensions
{
    public static MessageTypeRegistry SubscribeTo<TEvent>(this MessageTypeRegistry registry)
        where TEvent : IIntegrationEvent
    {
        registry.RegisterEvent<TEvent>();
        return registry;
    }

    public static MessageTypeRegistry HandleCommand<TCommand>(this MessageTypeRegistry registry)
        where TCommand : IIntegrationCommand
    {
        registry.RegisterCommand<TCommand>();
        return registry;
    }
}
