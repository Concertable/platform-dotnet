using Concertable.Messaging.Contracts;

namespace Concertable.Shared.Email.Application;

[MessageType("concertable.email.send-verification.v1")]
public sealed record SendVerificationEmailCommand(
    string To,
    string Token,
    string VerifyBaseUrl) : IIntegrationCommand;
