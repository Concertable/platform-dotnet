using Concertable.Messaging.Contracts;

namespace Concertable.Messaging.Infrastructure;

/// <summary>
/// Pauses and resumes every registered <see cref="IIngressQuiescer"/> together. <see cref="PauseAsync"/>
/// pauses each participant in turn and returns only once all have drained; if one fails it resumes those
/// already paused before rethrowing, so a failed pause never leaves the host half-quiesced.
/// <see cref="ResumeAsync"/> resumes them in the reverse order.
/// </summary>
internal sealed class HostQuiescence : IHostQuiescence
{
    private readonly IReadOnlyList<IIngressQuiescer> quiescers;

    public HostQuiescence(IEnumerable<IIngressQuiescer> quiescers)
    {
        this.quiescers = quiescers.ToList();
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
                await paused[i].ResumeAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task ResumeAsync(CancellationToken ct = default)
    {
        for (var i = quiescers.Count - 1; i >= 0; i--)
            await quiescers[i].ResumeAsync(ct);
    }
}
