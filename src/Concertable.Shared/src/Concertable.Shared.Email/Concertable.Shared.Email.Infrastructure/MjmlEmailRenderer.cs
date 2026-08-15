using Concertable.Shared.Email.Application;
using Mjml.Net;
using Scriban;

namespace Concertable.Shared.Email.Infrastructure;

internal sealed class MjmlEmailRenderer : IMjmlEmailRenderer
{
    private readonly MjmlRenderer mjml = new();

    public string Render<TModel>(string mjmlTemplate, TModel model)
    {
        var mjmlSource = Template.Parse(mjmlTemplate).Render(model, member => member.Name);
        return mjml.Render(mjmlSource).Html;
    }
}
