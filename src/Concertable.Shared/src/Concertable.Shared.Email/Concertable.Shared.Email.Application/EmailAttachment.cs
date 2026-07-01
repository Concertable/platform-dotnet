namespace Concertable.Shared.Email.Application;

public sealed record EmailAttachment(byte[] Content, string FileName, string MimeType = "application/pdf");
