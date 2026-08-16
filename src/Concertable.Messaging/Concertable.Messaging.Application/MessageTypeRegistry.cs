using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Application;

public sealed class MessageTypeRegistry
{
    private readonly Dictionary<string, Type> events = new();
    private readonly Dictionary<string, Type> commands = new();
    private readonly Dictionary<Type, string> commandDestinations = new();
    private readonly HashSet<Type> subscribedEvents = new();
    private readonly HashSet<Type> handledCommands = new();

    public IEnumerable<Type> SubscribedEventTypes => subscribedEvents;
    public IEnumerable<Type> HandledCommandTypes => handledCommands;

    public Type ResolveEvent(string messageType) => events[messageType];
    public Type ResolveCommand(string messageType) => commands[messageType];

    public bool TryResolveCommandDestination(Type commandType, out string? destinationServiceName) =>
        commandDestinations.TryGetValue(commandType, out destinationServiceName);

    public void RegisterEvent<TEvent>() where TEvent : IIntegrationEvent =>
        events[MessageTypeAttribute.Resolve(typeof(TEvent))] = typeof(TEvent);

    public void RegisterSubscription<TEvent>() where TEvent : IIntegrationEvent
    {
        RegisterEvent<TEvent>();
        subscribedEvents.Add(typeof(TEvent));
    }

    public void RegisterCommand<TCommand>() where TCommand : IIntegrationCommand =>
        commands[MessageTypeAttribute.Resolve(typeof(TCommand))] = typeof(TCommand);

    public void RegisterCommandSender<TCommand>(string destinationServiceName)
        where TCommand : IIntegrationCommand
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationServiceName);

        RegisterCommand<TCommand>();
        if (commandDestinations.TryGetValue(typeof(TCommand), out var existingDestination) &&
            !string.Equals(existingDestination, destinationServiceName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Command {typeof(TCommand).FullName} is already routed to service '{existingDestination}'.");
        }

        commandDestinations[typeof(TCommand)] = destinationServiceName;
    }

    public void RegisterCommandHandler<TCommand>() where TCommand : IIntegrationCommand
    {
        RegisterCommand<TCommand>();
        handledCommands.Add(typeof(TCommand));
    }
}
