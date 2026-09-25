using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Hosting;

namespace Concertable.Messaging.Infrastructure;

/// <summary>
/// Pauses every <see cref="IPausable"/> in the host together: the registered ones (such as HTTP requests)
/// plus every hosted service that is one (the ASB receiver, the outbox dispatcher). Hosted ones are found
/// through <see cref="IHostedService"/> so a host that removes such a service, as the integration harness
/// removes the ASB receiver, drops it here too instead of leaving a registration that resolves to nothing.
/// If one fails to pause, those already paused are resumed before the failure is rethrown; resume runs in
/// reverse order. Work arriving over an already-open connection (a WebSocket or SignalR message) never
/// re-enters the request pipeline, so a host that accepts such work must register its own
/// <see cref="IPausable"/> for it.
/// </summary>
public sealed class CompositePausable : IPausable
{
    private readonly IReadOnlyList<IPausable> pausables;

    public CompositePausable(IEnumerable<IPausable> pausables, IEnumerable<IHostedService> hostedServices)
    {
        this.pausables = pausables
            .Concat(hostedServices.OfType<IPausable>())
            .Distinct()
            .ToList();
    }

    public async Task PauseAsync(CancellationToken ct = default)
    {
        var paused = new List<IPausable>(pausables.Count);
        try
        {
            foreach (var pausable in pausables)
            {
                await pausable.PauseAsync(ct);
                paused.Add(pausable);
            }
        }
        catch
        {
            for (var i = paused.Count - 1; i >= 0; i--)
            {
                // Best-effort rollback: one failing to resume must not stop the rest, nor replace the pause
                // failure the caller needs to see.
                try { await paused[i].ResumeAsync(CancellationToken.None); }
                catch { }
            }
            throw;
        }
    }

    public async Task ResumeAsync(CancellationToken ct = default)
    {
        for (var i = pausables.Count - 1; i >= 0; i--)
            await pausables[i].ResumeAsync(ct);
    }
}
