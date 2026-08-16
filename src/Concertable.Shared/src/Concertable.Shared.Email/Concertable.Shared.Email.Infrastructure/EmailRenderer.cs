using Concertable.Shared.Email.Application;
using Mjml.Net;
using Scriban;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class EmailRenderer : IEmailRenderer
{
    private readonly MjmlRenderer mjml = new();

    public RenderedEmail Render(IEmailContent content)
    {
        var mjmlSource = Template.Parse(content.Template).Render(content, member => member.Name);
        return new RenderedEmail(content.Subject, mjml.Render(mjmlSource).Html);
    }
}
