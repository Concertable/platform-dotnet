namespace Concertable.Kernel.UnitTests;

public sealed class StateMachineTests
{
    private readonly StateMachine<State, Trigger> stateMachine;

    public StateMachineTests()
    {
        this.stateMachine = new StateMachine<State, Trigger>(
        [
            (State.Pending, Trigger.Confirm, State.Confirmed),
            (State.Confirmed, Trigger.Cancel, State.Cancelled)
        ]);
    }

    [Fact]
    public void Transition_DefinedEdge_ReturnsNextState()
    {
        var result = this.stateMachine.Transition(State.Pending, Trigger.Confirm);

        Assert.True(result.TryGetValue(out var next));
        Assert.Equal(State.Confirmed, next);
    }

    [Fact]
    public void Transition_UndefinedEdge_ReturnsTypedError()
    {
        var result = this.stateMachine.Transition(State.Pending, Trigger.Cancel);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new TransitionError<State, Trigger>(State.Pending, Trigger.Cancel), error);
    }

    [Fact]
    public void Constructor_DuplicateEdge_ThrowsArgumentException()
    {
        var transitions = new (State Current, Trigger Trigger, State Next)[]
        {
            (State.Pending, Trigger.Confirm, State.Confirmed),
            (State.Pending, Trigger.Confirm, State.Cancelled)
        };

        Assert.Throws<ArgumentException>(() => new StateMachine<State, Trigger>(transitions));
    }

    [Fact]
    public void Transition_SourceCollectionMutates_KeepsInitialSnapshot()
    {
        var transitions = new List<(State Current, Trigger Trigger, State Next)>
        {
            (State.Pending, Trigger.Confirm, State.Confirmed)
        };
        var stateMachine = new StateMachine<State, Trigger>(transitions);

        transitions.Clear();
        transitions.Add((State.Pending, Trigger.Confirm, State.Cancelled));
        var result = stateMachine.Transition(State.Pending, Trigger.Confirm);

        Assert.True(result.TryGetValue(out var next));
        Assert.Equal(State.Confirmed, next);
    }

    [Fact]
    public void Transition_ConcurrentReads_ReturnExpectedState()
    {
        var successes = new bool[1_000];
        var states = new State[1_000];

        Parallel.For(0, successes.Length, index =>
        {
            var result = this.stateMachine.Transition(State.Pending, Trigger.Confirm);
            successes[index] = result.TryGetValue(out states[index]);
        });

        Assert.All(successes, Assert.True);
        Assert.All(states, state => Assert.Equal(State.Confirmed, state));
    }

    private enum State
    {
        Pending,
        Confirmed,
        Cancelled
    }

    private enum Trigger
    {
        Confirm,
        Cancel
    }
}
