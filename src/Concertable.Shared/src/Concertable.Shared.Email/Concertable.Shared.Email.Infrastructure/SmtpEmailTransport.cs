using Concertable.Shared.Email.Application;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class SmtpEmailTransport : IEmailTransport, IEmailSender
{
    private readonly IConfiguration configuration;

    public SmtpEmailTransport(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyList<EmailAttachment>? attachments = null)
    {
        var email = new MimeMessage();

        email.From.Add(new MailboxAddress("Concertable", configuration["Email:From"]));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };

        if (attachments is not null)
            foreach (var attachment in attachments)
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.MimeType));

        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            configuration["Email:SmtpServer"],
            int.Parse(configuration["Email:SmtpPort"]!),
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            configuration["Email:Username"],
            configuration["Email:Password"]);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public Task SendVerificationAsync(string toEmail, string token, string verifyBaseUrl, CancellationToken ct = default)
    {
        var link = $"{verifyBaseUrl}?token={Uri.EscapeDataString(token)}";
        return SendEmailAsync(toEmail, "Verify your email", $"Click here to verify your email: {link}");
    }
}
