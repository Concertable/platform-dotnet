using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.Shared.Email.Infrastructure;

/// <summary>
/// The business-facing <see cref="IEmailSender"/>: instead of sending inline, it stages the send as an
/// integration command on the caller's ambient transaction, so the email commits atomically with the
/// producing business change and is delivered off-thread by the command handler (with retry).
/// </summary>
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
