using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class SendEmailCommandHandler : IIntegrationCommandHandler<SendEmailCommand>
{
    private readonly IEmailTransport transport;

    public SendEmailCommandHandler(IEmailTransport transport)
    {
        this.transport = transport;
    }

    public Task HandleAsync(SendEmailCommand command, MessageEnvelope envelope, CancellationToken ct = default) =>
        transport.SendEmailAsync(command.To, command.Subject, command.Body, command.Attachments);
}
