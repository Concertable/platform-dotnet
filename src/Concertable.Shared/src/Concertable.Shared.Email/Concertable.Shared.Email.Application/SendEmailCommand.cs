using Concertable.Messaging.Contracts;

namespace Concertable.Shared.Email.Application;

[MessageType("concertable.email.send.v1")]
public sealed record SendEmailCommand(
    string To,
    string Subject,
    string Body,
    IReadOnlyList<EmailAttachment>? Attachments = null) : IIntegrationCommand;
