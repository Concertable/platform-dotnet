using Concertable.Messaging.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.Messaging.AspNetCore;

/// <summary>
/// The HTTP request pipeline as an <see cref="IIngressQuiescer"/>. <see cref="RequestQuiescenceMiddleware"/>
/// registers each gated request here for the life of its handler; while paused, a newly arriving request
/// waits for <see cref="ResumeAsync"/> before its handler runs. <see cref="PauseAsync"/> excludes the request
/// that is itself driving the pause — read from <see cref="IHttpContextAccessor"/> — then returns once every
/// other in-flight request has finished, so the caller can rely on no request touching the database until
/// resume. Call <see cref="PauseAsync"/> from within that request's own handler (inline, so its
/// <see cref="HttpContext"/> flows through <see cref="IHttpContextAccessor"/>); dispatching the pause onto a
/// detached execution context would lose the self-exclusion. Overlapping pause/resume calls are safe: a
/// second pause joins the first's drain, and a resume releases any pause still waiting.
/// </summary>
internal sealed class HttpIngressQuiescer : IIngressQuiescer
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly object gate = new();
    private readonly HashSet<HttpContext> inFlight = new();
    private bool paused;
    private TaskCompletionSource resume = CreateCompletedSource();
    private TaskCompletionSource drained = CreateCompletedSource();

    public HttpIngressQuiescer(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    internal async Task EnterAsync(HttpContext context, CancellationToken ct)
    {
        while (true)
        {
            Task wait;
            lock (gate)
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
        lock (gate)
        {
            if (inFlight.Remove(context) && paused && inFlight.Count == 0)
                drained.TrySetResult();
        }
    }

    public async Task PauseAsync(CancellationToken ct = default)
    {
        Task wait;
        lock (gate)
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
        lock (gate)
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
