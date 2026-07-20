using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class SendVerificationEmailCommandHandler : IIntegrationCommandHandler<SendVerificationEmailCommand>
{
    private readonly IEmailTransport transport;

    public SendVerificationEmailCommandHandler(IEmailTransport transport)
    {
        this.transport = transport;
    }

    public Task HandleAsync(SendVerificationEmailCommand command, MessageEnvelope envelope, CancellationToken ct = default) =>
        transport.SendVerificationAsync(command.To, command.Token, command.VerifyBaseUrl, ct);
}
