namespace Concertable.Messaging.Contracts;

/// <summary>
/// Brings a whole host to a quiescent state by pausing every registered <see cref="IIngressQuiescer"/>.
/// <see cref="PauseAsync"/> returns only once all of them have stopped admitting work and drained what was
/// already in flight, so a caller can rely on nothing in the process starting or continuing database work
/// until <see cref="ResumeAsync"/>. Used by a host that must reach quiescence, such as an end-to-end test
/// host resetting its database between tests. It cannot cover work delivered over an already-established
/// persistent connection (a WebSocket or SignalR message never re-enters the request pipeline); a host that
/// admits such work during a reset must pause it through its own <see cref="IIngressQuiescer"/>.
/// </summary>
public interface IHostQuiescence
{
    Task PauseAsync(CancellationToken ct = default);

    Task ResumeAsync(CancellationToken ct = default);
}
