using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AspNetCore;

internal sealed class PausableRequestsMiddleware : IMiddleware
{
    private readonly PausableRequests requests;
    private readonly PausableRequestsOptions options;

    public PausableRequestsMiddleware(PausableRequests requests, IOptions<PausableRequestsOptions> options)
    {
        this.requests = requests;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.WebSockets.IsWebSocketRequest || options.IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        await requests.EnterAsync(context, context.RequestAborted);
        try
        {
            await next(context);
        }
        finally
        {
            requests.Exit(context);
        }
    }
}
