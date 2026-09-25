using Concertable.Messaging.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.AspNetCore;

/// <summary>
/// Holds incoming requests while the host is paused and tracks the ones in flight. While paused, a newly
/// arriving request waits for <see cref="ResumeAsync"/> before its handler runs. <see cref="PauseAsync"/>
/// excludes the request that is itself driving the pause — read from <see cref="IHttpContextAccessor"/> —
/// then returns once every other in-flight request has finished. Call <see cref="PauseAsync"/> from within
/// that request's own handler (inline, so its <see cref="HttpContext"/> flows through
/// <see cref="IHttpContextAccessor"/>); dispatching the pause onto a detached execution context would lose
/// the self-exclusion. Overlapping pause/resume calls are safe: a second pause joins the first's drain, and a
/// resume releases any pause still waiting.
/// </summary>
internal sealed class GateMiddleware : IMiddleware, IPausable
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly GateOptions options;
    private readonly object sync = new();
    private readonly HashSet<HttpContext> inFlight = new();
    private bool paused;
    private TaskCompletionSource resume = CreateCompletedSource();
    private TaskCompletionSource drained = CreateCompletedSource();

    public GateMiddleware(IHttpContextAccessor httpContextAccessor, IOptions<GateOptions> options)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.WebSockets.IsWebSocketRequest || options.IsExempt(context.Request.Path))
        {
            await next(context);
            return;
        }

        await EnterAsync(context, context.RequestAborted);
        try
        {
            await next(context);
        }
        finally
        {
            Exit(context);
        }
    }

    internal async Task EnterAsync(HttpContext context, CancellationToken ct)
    {
        while (true)
        {
            Task wait;
            lock (sync)
            {
                if (!paused)
                {
                    inFlight.Add(context);
                    return;
                }
                wait = resume.Task;
            }
            await wait.WaitAsync(ct);
        }
    }

    internal void Exit(HttpContext context)
    {
        lock (sync)
        {
            if (inFlight.Remove(context) && paused && inFlight.Count == 0)
                drained.TrySetResult();
        }
    }

    public async Task PauseAsync(CancellationToken ct = default)
    {
        Task wait;
        lock (sync)
        {
            if (!paused)
            {
                paused = true;
                resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            if (httpContextAccessor.HttpContext is { } pauser)
                inFlight.Remove(pauser);

            if (inFlight.Count == 0)
                drained.TrySetResult();

            wait = drained.Task;
        }
        await wait.WaitAsync(ct);
    }

    public Task ResumeAsync(CancellationToken ct = default)
    {
        lock (sync)
        {
            paused = false;
            resume.TrySetResult();
            drained.TrySetResult();
        }
        return Task.CompletedTask;
    }

    private static TaskCompletionSource CreateCompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}
