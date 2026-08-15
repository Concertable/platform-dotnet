namespace Concertable.Shared.Email.Application;

/// <summary>
/// Renders a transactional email's HTML body from an MJML template and a model. The model is bound into
/// the template, then the MJML is compiled to Outlook-safe, CSS-inlined HTML. Business code owns the
/// template and the model; this is the shared rendering mechanism, the email counterpart of
/// <c>IPdfRenderer</c>.
/// </summary>
public interface IEmailRenderer
{
    /// <param name="mjmlTemplate">MJML markup with template placeholders bound against <paramref name="model"/>.</param>
    /// <param name="model">The typed view model whose members the template references.</param>
    string Render(string mjmlTemplate, object model);
}
