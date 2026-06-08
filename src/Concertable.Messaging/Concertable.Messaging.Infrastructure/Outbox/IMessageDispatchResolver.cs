using Concertable.Messaging.Domain;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal interface IMessageDispatchResolver
{
    IMessageDispatcher Resolve(MessageKind kind);
}
