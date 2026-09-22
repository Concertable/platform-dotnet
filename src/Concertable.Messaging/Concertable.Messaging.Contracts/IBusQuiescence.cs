namespace Concertable.Messaging.Contracts;

/// <summary>
/// Pauses and resumes inbound message consumption. <see cref="PauseAsync"/> returns only once every
/// handler already running has finished, so a caller holding the pause can rely on no handler touching
/// its database. Implemented by the receiving transport and used by a host that must reach a quiescent
/// state, such as an end-to-end test host resetting its database between tests.
/// </summary>
public interface IBusQuiescence
{
    Task PauseAsync(CancellationToken ct = default);

    Task ResumeAsync(CancellationToken ct = default);
}
