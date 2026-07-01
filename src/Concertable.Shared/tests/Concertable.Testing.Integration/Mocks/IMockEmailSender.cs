using Concertable.Shared.Email.Application;

namespace Concertable.Testing.Integration.Mocks;

public interface IMockEmailSender : IEmailSender, IResettable
{
    IReadOnlyList<SentEmail> Sent { get; }
    string? ExtractToken(string email);
}
