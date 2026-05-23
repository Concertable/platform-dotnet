namespace Concertable.Messaging.Contracts;

public interface IIntegrationCommandHandler<TCommand> where TCommand : IIntegrationCommand
{
    Task HandleAsync(TCommand command, MessageEnvelope envelope, CancellationToken ct = default);
}
