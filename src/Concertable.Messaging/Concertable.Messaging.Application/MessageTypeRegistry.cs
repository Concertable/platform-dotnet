using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Application;

public sealed class MessageTypeRegistry
{
    private readonly Dictionary<string, Type> events = new();
    private readonly Dictionary<string, Type> commands = new();
    private readonly HashSet<Type> subscribedEvents = new();

    public IEnumerable<Type> SubscribedEventTypes => subscribedEvents;
    public IEnumerable<Type> RegisteredCommandTypes => commands.Values;

    public Type ResolveEvent(string messageType) => events[messageType];
    public Type ResolveCommand(string messageType) => commands[messageType];

    public void RegisterEvent<TEvent>() where TEvent : IIntegrationEvent =>
        events[MessageTypeAttribute.Resolve(typeof(TEvent))] = typeof(TEvent);

    public void RegisterSubscription<TEvent>() where TEvent : IIntegrationEvent
    {
        RegisterEvent<TEvent>();
        subscribedEvents.Add(typeof(TEvent));
    }

    public void RegisterCommand<TCommand>() where TCommand : IIntegrationCommand =>
        commands[MessageTypeAttribute.Resolve(typeof(TCommand))] = typeof(TCommand);
}
