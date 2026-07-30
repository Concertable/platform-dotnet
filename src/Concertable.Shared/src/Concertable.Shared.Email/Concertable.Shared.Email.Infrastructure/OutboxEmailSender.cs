using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class OutboxEmailSender : IEmailSender
{
    private readonly IBus bus;

    public OutboxEmailSender(IBus bus)
    {
        this.bus = bus;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyList<EmailAttachment>? attachments = null) =>
        bus.SendAsync(new SendEmailCommand(toEmail, subject, body, attachments));

    public Task SendVerificationAsync(string toEmail, string token, string verifyBaseUrl, CancellationToken ct = default) =>
        bus.SendAsync(new SendVerificationEmailCommand(toEmail, token, verifyBaseUrl), ct);
}
