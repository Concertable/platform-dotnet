namespace Concertable.Shared.Email.Application;

/// <summary>
/// Renders a transactional email from its typed content — binds the content's data into its body
/// template and compiles it to Outlook-safe, CSS-inlined HTML. The registered renderer defines the
/// template language (MJML today).
/// </summary>
public interface IEmailRenderer
{
    RenderedEmail Render(IEmailContent content);
}

/// <summary>
/// One email's subject and body template, plus (on the concrete type) the typed data both bind against.
/// <see cref="Subject"/> is built in plain C#; <see cref="Template"/> is body markup in the registered
/// renderer's language.
/// </summary>
public interface IEmailContent
{
    string Subject { get; }
    string Template { get; }
}

public sealed record RenderedEmail(string Subject, string HtmlBody);
