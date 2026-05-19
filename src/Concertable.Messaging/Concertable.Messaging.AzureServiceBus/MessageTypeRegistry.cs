namespace Concertable.Messaging.AzureServiceBus;

public sealed class MessageTypeRegistry
{
    private readonly Dictionary<string, Type> events = new();
    private readonly Dictionary<string, Type> commands = new();

    public IEnumerable<Type> RegisteredEventTypes => events.Values;
    public IEnumerable<Type> RegisteredCommandTypes => commands.Values;

    public Type ResolveEvent(string messageType) => events[messageType];
    public Type ResolveCommand(string messageType) => commands[messageType];

    public void RegisterEvent<TEvent>() where TEvent : IIntegrationEvent =>
        events[typeof(TEvent).FullName!] = typeof(TEvent);

    public void RegisterCommand<TCommand>() where TCommand : IIntegrationCommand =>
        commands[typeof(TCommand).FullName!] = typeof(TCommand);
}
