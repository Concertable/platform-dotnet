using Concertable.Shared.Email.Application;

namespace Concertable.Testing.Integration.Mocks;

public interface IMockEmailSender : IEmailTransport, IResettable
{
    IReadOnlyList<SentEmail> Sent { get; }
    string? ExtractToken(string email);
}
