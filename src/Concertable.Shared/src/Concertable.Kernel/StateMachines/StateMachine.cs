using System.Collections.Frozen;
using Reunion;

namespace Concertable.Kernel;

public class StateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : notnull
    where TTrigger : notnull
{
    private readonly FrozenDictionary<(TState State, TTrigger Trigger), TState> transitions;

    public StateMachine(IEnumerable<(TState Current, TTrigger Trigger, TState Next)> transitions)
    {
        this.transitions = transitions.ToFrozenDictionary(
            transition => (transition.Current, transition.Trigger),
            transition => transition.Next);
    }

    public Result<TState, TransitionError<TState, TTrigger>> Transition(
        TState current,
        TTrigger trigger) =>
        this.transitions.TryGetValue((current, trigger), out var next)
            ? next
            : new TransitionError<TState, TTrigger>(current, trigger);
}

public sealed class ConfiguredStateMachine<TState, TTrigger>(
    IEnumerable<(TState Current, TTrigger Trigger, TState Next)> transitions)
    : StateMachine<TState, TTrigger>(transitions)
    where TState : notnull
    where TTrigger : notnull;
