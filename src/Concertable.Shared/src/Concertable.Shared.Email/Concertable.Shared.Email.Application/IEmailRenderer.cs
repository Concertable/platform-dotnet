namespace Concertable.Shared.Email.Application;

/// <summary>
/// Binds a typed model into an MJML template and compiles it to Outlook-safe, CSS-inlined HTML — the
/// email counterpart of <c>IPdfRenderer</c>. The caller owns the template (typically an embedded
/// <c>.mjml</c> resource) and the model; the subject is built in plain C# at the call site, not here.
/// </summary>
public interface IEmailRenderer
{
    string Render<TModel>(string mjmlTemplate, TModel model);
}
