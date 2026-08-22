namespace Concertable.Kernel;

public sealed record TransitionError<TState, TTrigger>(
    TState Current,
    TTrigger Trigger);
