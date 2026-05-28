using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.UnitTests;

[MessageType("concertable.messaging.fake-integration-event.v1")]
public sealed record FakeIntegrationEvent(Guid Id, string Name, int Count) : IIntegrationEvent;

[MessageType("concertable.messaging.fake-integration-command.v1")]
public sealed record FakeIntegrationCommand(Guid Id, string Reason) : IIntegrationCommand;

[MessageType("concertable.messaging.other-fake-event.v1")]
public sealed record OtherFakeEvent(string Tag) : IIntegrationEvent;
