using Reunion;

namespace Concertable.Kernel;

public interface IStateMachine<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    Result<TState, TransitionError<TState, TTrigger>> Transition(
        TState current,
        TTrigger trigger);
}
