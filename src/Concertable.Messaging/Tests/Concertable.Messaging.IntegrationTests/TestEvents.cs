using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.IntegrationTests;

[MessageType("concertable.messaging.fake-integration-event.v1")]
public sealed record FakeIntegrationEvent(Guid Id, string Name, int Count) : IIntegrationEvent;
