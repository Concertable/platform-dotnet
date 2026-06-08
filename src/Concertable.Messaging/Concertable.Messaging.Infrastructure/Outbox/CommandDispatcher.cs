using System.Reflection;
using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class CommandDispatcher : IMessageDispatcher
{
    private static readonly MethodInfo SendMethod =
        typeof(IBusTransport).GetMethod(nameof(IBusTransport.SendAsync))!;

    private readonly MessageTypeRegistry registry;
    private readonly IBusTransport transport;

    public CommandDispatcher(MessageTypeRegistry registry, IBusTransport transport)
    {
        this.registry = registry;
        this.transport = transport;
    }

    public Type ResolveType(string messageType) => registry.ResolveCommand(messageType);

    public Task DispatchAsync(object message, MessageEnvelope envelope, CancellationToken ct) =>
        (Task)SendMethod.MakeGenericMethod(message.GetType()).Invoke(transport, [message, envelope, ct])!;
}
