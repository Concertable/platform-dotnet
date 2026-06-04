using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal interface IMessageDispatcher
{
    Type ResolveType(string messageType);
    Task DispatchAsync(object message, MessageEnvelope envelope, CancellationToken ct);
}
