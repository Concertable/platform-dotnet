namespace Concertable.Messaging.Contracts;

/// <summary>
/// Something that starts work on its own and can be held still. <see cref="PauseAsync"/> returns only once it
/// has stopped starting new work and everything it already started has finished, so the caller can rely on it
/// doing nothing until <see cref="ResumeAsync"/>.
/// </summary>
public interface IPausable
{
    Task PauseAsync(CancellationToken ct = default);

    Task ResumeAsync(CancellationToken ct = default);
}
