namespace Concertable.Shared.Email.Application;

/// <summary>
/// Binds a typed model into an MJML template (Scriban) and compiles it to Outlook-safe, CSS-inlined HTML.
/// The caller owns the template (typically an embedded <c>.mjml</c> resource) and builds the subject in
/// plain C# at the call site.
/// </summary>
public interface IMjmlEmailRenderer
{
    string Render<TModel>(string mjmlTemplate, TModel model);
}
