using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Hosting;

namespace Concertable.Messaging.Infrastructure;

/// <summary>
/// Pauses and resumes every ingress participant together. Participants are the explicitly-registered
/// <see cref="IIngressQuiescer"/>s (such as the HTTP request pipeline) plus every hosted service that is one
/// (the ASB receiver, the outbox dispatcher) — discovered through <see cref="IHostedService"/> so a host that
/// removes such a service, as the integration harness removes the ASB receiver, drops it from quiescence
/// automatically rather than leaving a registration that resolves to nothing. <see cref="PauseAsync"/> pauses
/// each in turn and returns only once all have drained; if one fails it resumes those already paused before
/// rethrowing, so a failed pause never leaves the host half-quiesced. <see cref="ResumeAsync"/> resumes them
/// in the reverse order.
/// </summary>
internal sealed class HostQuiescence : IHostQuiescence
{
    private readonly IReadOnlyList<IIngressQuiescer> quiescers;

    public HostQuiescence(IEnumerable<IIngressQuiescer> quiescers, IEnumerable<IHostedService> hostedServices)
    {
        this.quiescers = quiescers
            .Concat(hostedServices.OfType<IIngressQuiescer>())
            .Distinct()
            .ToList();
    }

    public async Task PauseAsync(CancellationToken ct = default)
    {
        var paused = new List<IIngressQuiescer>(quiescers.Count);
        try
        {
            foreach (var quiescer in quiescers)
            {
                await quiescer.PauseAsync(ct);
                paused.Add(quiescer);
            }
        }
        catch
        {
            for (var i = paused.Count - 1; i >= 0; i--)
            {
                // Best-effort rollback: one participant failing to resume must not stop the rest, nor
                // replace the pause failure the caller needs to see.
                try { await paused[i].ResumeAsync(CancellationToken.None); }
                catch { }
            }
            throw;
        }
    }

    public async Task ResumeAsync(CancellationToken ct = default)
    {
        for (var i = quiescers.Count - 1; i >= 0; i--)
            await quiescers[i].ResumeAsync(ct);
    }
}
