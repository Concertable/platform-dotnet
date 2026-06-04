using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class MessageDispatchResolver : IMessageDispatchResolver
{
    private readonly EventDispatcher events;
    private readonly CommandDispatcher commands;

    public MessageDispatchResolver(EventDispatcher events, CommandDispatcher commands)
    {
        this.events = events;
        this.commands = commands;
    }

    public IMessageDispatcher Resolve(MessageKind kind) => kind switch
    {
        MessageKind.Event => events,
        MessageKind.Command => commands,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
