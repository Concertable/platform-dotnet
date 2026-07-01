using Concertable.Shared.Email.Application;

namespace Concertable.Testing.Integration.Mocks;

public sealed record SentEmail(string To, string Subject, string Body);

public sealed class MockEmailSender : IMockEmailSender
{
    private readonly List<SentEmail> sent = new();
    public IReadOnlyList<SentEmail> Sent => sent;

    public Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyList<EmailAttachment>? attachments = null)
    {
        sent.Add(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }

    public Task SendVerificationAsync(string toEmail, string token, string verifyBaseUrl, CancellationToken ct = default)
    {
        var link = $"{verifyBaseUrl}?token={Uri.EscapeDataString(token)}";
        sent.Add(new SentEmail(toEmail, "Verify your email", $"Click here to verify your email: {link}"));
        return Task.CompletedTask;
    }

    public void Reset() => sent.Clear();

    public string? ExtractToken(string email)
    {
        var msg = sent.FirstOrDefault(m => m.To == email);
        if (msg is null) return null;

        var index = msg.Body.IndexOf("http", StringComparison.Ordinal);
        if (index < 0) return null;

        var uri = msg.Body[index..].Split('\n', ' ').First().Trim();
        var query = new Uri(uri).Query;
        var token = System.Web.HttpUtility.ParseQueryString(query)["token"];
        return token;
    }
}
