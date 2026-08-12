using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Application;

public sealed class MessageTypeRegistry
{
    private readonly Dictionary<string, Type> events = new();
    private readonly Dictionary<string, Type> commands = new();
    private readonly HashSet<Type> subscribedEvents = new();
    private readonly HashSet<Type> handledCommands = new();

    public IEnumerable<Type> SubscribedEventTypes => subscribedEvents;
    public IEnumerable<Type> RegisteredCommandTypes => handledCommands;

    public Type ResolveEvent(string messageType) => events[messageType];
    public Type ResolveCommand(string messageType) => commands[messageType];

    public void RegisterEvent<TEvent>() where TEvent : IIntegrationEvent =>
        events[MessageTypeAttribute.Resolve(typeof(TEvent))] = typeof(TEvent);

    public void RegisterSubscription<TEvent>() where TEvent : IIntegrationEvent
    {
        RegisterEvent<TEvent>();
        subscribedEvents.Add(typeof(TEvent));
    }

    public void RegisterCommandForSending<TCommand>() where TCommand : IIntegrationCommand =>
        commands[MessageTypeAttribute.Resolve(typeof(TCommand))] = typeof(TCommand);

    public void RegisterCommand<TCommand>() where TCommand : IIntegrationCommand
    {
        RegisterCommandForSending<TCommand>();
        handledCommands.Add(typeof(TCommand));
    }
}
