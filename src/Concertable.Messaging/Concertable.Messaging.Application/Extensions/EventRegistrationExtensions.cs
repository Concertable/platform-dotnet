using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Application.Extensions;

public static class EventRegistrationExtensions
{
    extension(MessageTypeRegistry registry)
    {
        public MessageTypeRegistry Publishes<TEvent>()
            where TEvent : IIntegrationEvent
        {
            registry.RegisterEvent<TEvent>();
            return registry;
        }

        public MessageTypeRegistry SubscribeTo<TEvent>()
            where TEvent : IIntegrationEvent
        {
            registry.RegisterSubscription<TEvent>();
            return registry;
        }

        public MessageTypeRegistry HandleCommand<TCommand>()
            where TCommand : IIntegrationCommand
        {
            registry.RegisterCommandHandler<TCommand>();
            return registry;
        }

        public MessageTypeRegistry Sends<TCommand>()
            where TCommand : IIntegrationCommand
        {
            registry.RegisterCommand<TCommand>();
            return registry;
        }

        public MessageTypeRegistry SendsTo<TCommand>(string destinationServiceName)
            where TCommand : IIntegrationCommand
        {
            registry.RegisterCommandSender<TCommand>(destinationServiceName);
            return registry;
        }
    }
}
