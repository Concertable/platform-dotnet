using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Application.Extensions;

public static class EventRegistrationExtensions
{
    public static MessageTypeRegistry Publishes<TEvent>(this MessageTypeRegistry registry)
        where TEvent : IIntegrationEvent
    {
        registry.RegisterEvent<TEvent>();
        return registry;
    }

    public static MessageTypeRegistry SubscribeTo<TEvent>(this MessageTypeRegistry registry)
        where TEvent : IIntegrationEvent
    {
        registry.RegisterSubscription<TEvent>();
        return registry;
    }

    public static MessageTypeRegistry HandleCommand<TCommand>(this MessageTypeRegistry registry)
        where TCommand : IIntegrationCommand
    {
        registry.RegisterCommand<TCommand>();
        return registry;
    }
}
