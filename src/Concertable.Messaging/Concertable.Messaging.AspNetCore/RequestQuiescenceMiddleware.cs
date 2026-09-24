using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AspNetCore;

internal sealed class RequestQuiescenceMiddleware : IMiddleware
{
    private readonly HttpIngressQuiescer quiescer;
    private readonly HttpQuiescenceOptions options;

    public RequestQuiescenceMiddleware(HttpIngressQuiescer quiescer, IOptions<HttpQuiescenceOptions> options)
    {
        this.quiescer = quiescer;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.WebSockets.IsWebSocketRequest || options.IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        await quiescer.EnterAsync(context, context.RequestAborted);
        try
        {
            await next(context);
        }
        finally
        {
            quiescer.Exit(context);
        }
    }
}
