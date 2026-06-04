using System.Reflection;
using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class EventDispatcher : IMessageDispatcher
{
    private static readonly MethodInfo PublishMethod =
        typeof(IBusTransport).GetMethod(nameof(IBusTransport.PublishAsync))!;

    private readonly MessageTypeRegistry registry;
    private readonly IBusTransport transport;

    public EventDispatcher(MessageTypeRegistry registry, IBusTransport transport)
    {
        this.registry = registry;
        this.transport = transport;
    }

    public Type ResolveType(string messageType) => registry.ResolveEvent(messageType);

    public Task DispatchAsync(object message, MessageEnvelope envelope, CancellationToken ct) =>
        (Task)PublishMethod.MakeGenericMethod(message.GetType()).Invoke(transport, [message, envelope, ct])!;
}
