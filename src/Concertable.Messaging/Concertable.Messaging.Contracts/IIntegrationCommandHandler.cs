namespace Concertable.Messaging;

public interface IIntegrationCommandHandler<TCommand> where TCommand : IIntegrationCommand
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}
