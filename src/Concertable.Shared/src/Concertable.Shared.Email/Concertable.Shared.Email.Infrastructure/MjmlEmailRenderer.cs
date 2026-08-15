using Concertable.Shared.Email.Application;
using Mjml.Net;
using Scriban;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class MjmlEmailRenderer : IEmailRenderer
{
    private readonly MjmlRenderer mjml = new();

    public string Render(string mjmlTemplate, object model)
    {
        var mjmlSource = Template.Parse(mjmlTemplate).Render(model, member => member.Name);
        return mjml.Render(mjmlSource).Html;
    }
}
