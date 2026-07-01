using Microsoft.Extensions.Logging;

namespace Concertable.Shared.Email.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "[FakeEmail] To: {Email} | Subject: {Subject} | Attachments: {Count}\n{Body}")]
    internal static partial void FakeEmailSent(this ILogger logger, string email, string subject, int count, string body);

    [LoggerMessage(Level = LogLevel.Information, Message = "[FakeEmail] Verification email skipped for {Email}")]
    internal static partial void FakeVerificationEmailSkipped(this ILogger logger, string email);
}
