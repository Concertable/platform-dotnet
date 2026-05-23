using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.UnitTests;

public sealed record FakeIntegrationEvent(Guid Id, string Name, int Count) : IIntegrationEvent;

public sealed record FakeIntegrationCommand(Guid Id, string Reason) : IIntegrationCommand;

public sealed record OtherFakeEvent(string Tag) : IIntegrationEvent;
