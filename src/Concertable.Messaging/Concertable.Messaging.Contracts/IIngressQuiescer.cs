namespace Concertable.Messaging.Contracts;

/// <summary>
/// One source of work that can start or continue database activity inside a host — a transport receiver, a
/// polling dispatcher, or the HTTP request pipeline. <see cref="PauseAsync"/> returns only once the source
/// has stopped admitting new work and every unit it already admitted has finished, so a caller holding the
/// pause can rely on that source touching nothing until <see cref="ResumeAsync"/>. Composed by
/// <see cref="IHostQuiescence"/>, which pauses every registered participant together.
/// </summary>
public interface IIngressQuiescer
{
    Task PauseAsync(CancellationToken ct = default);

    Task ResumeAsync(CancellationToken ct = default);
}
