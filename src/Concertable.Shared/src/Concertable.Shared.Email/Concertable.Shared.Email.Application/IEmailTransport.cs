namespace Concertable.Shared.Email.Application;

/// <summary>
/// The actual email delivery mechanism (SMTP, or the dev/E2E fake). Invoked by the command handlers
/// off the request thread — business code sends through <see cref="IEmailSender"/>, never this.
/// </summary>
public interface IEmailTransport
{
    Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyList<EmailAttachment>? attachments = null);

    Task SendVerificationAsync(string toEmail, string token, string verifyBaseUrl, CancellationToken ct = default);
}
