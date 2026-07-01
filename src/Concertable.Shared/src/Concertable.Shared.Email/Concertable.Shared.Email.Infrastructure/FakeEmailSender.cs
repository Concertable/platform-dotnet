using Concertable.Shared.Email.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class FakeEmailSender : IEmailSender
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<FakeEmailSender> logger;

    public FakeEmailSender(IHttpClientFactory httpClientFactory, ILogger<FakeEmailSender> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, IReadOnlyList<EmailAttachment>? attachments = null)
    {
        var count = attachments?.Count ?? 0;
        logger.FakeEmailSent(toEmail, subject, count, body);
        return Task.CompletedTask;
    }

    public async Task SendVerificationAsync(string toEmail, string token, string verifyBaseUrl, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient();
        await client.GetAsync($"{verifyBaseUrl}?token={Uri.EscapeDataString(token)}", ct);
    }
}

