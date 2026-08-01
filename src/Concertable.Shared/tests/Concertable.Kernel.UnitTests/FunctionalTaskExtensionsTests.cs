using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class FunctionalTaskExtensionsTests
{
    [Fact]
    public async Task ResultTaskSynchronousExtensions_EveryOperation_ComposesSourceTask()
    {
        var successTap = 0;
        var failureTap = string.Empty;
        var success = Task.FromResult(Result.Success<int, string>(2));
        var failure = Task.FromResult(Result.Failure<int, string>("error"));

        Assert.Equal("2", await success.Match(value => value.ToString(), error => error));
        Assert.Equal(Result.Success<string, string>("2"), await success.Map(value => value.ToString()));
        Assert.Equal(Result.Success<string, string>("2"), await success.Bind(value => Result.Success<string, string>(value.ToString())));
        Assert.Equal(Result.Failure<int, int>(5), await failure.MapError(error => error.Length));
        Assert.Equal(Result.Failure<int, string>("invalid"), await success.Ensure(_ => false, () => "invalid"));
        Assert.Equal(Result.Success<int, string>(2), await success.Tap(value => successTap = value));
        Assert.Equal(Result.Failure<int, string>("error"), await failure.TapError(error => failureTap = error));
        Assert.Equal(Result.Success<int, string>(5), await failure.Recover(error => error.Length));
        Assert.Equal(Result.Success<int, string>(5), await failure.RecoverWith(error => Result.Success<int, string>(error.Length)));
        Assert.Equal(2, successTap);
        Assert.Equal("error", failureTap);
    }

    [Fact]
    public async Task OptionTaskSynchronousExtensions_EveryOperation_ComposesSourceTask()
    {
        var some = Task.FromResult(Option.Some(2));
        var none = Task.FromResult(Option.None<int>());

        Assert.Equal("2", await some.Match(value => value.ToString(), () => "none"));
        Assert.Equal(Option.Some("2"), await some.Map(value => value.ToString()));
        Assert.Equal(Option.Some("2"), await some.Bind(value => Option.Some(value.ToString())));
        Assert.Equal(Option.Some(3), await none.OrElse(() => Option.Some(3)));
        Assert.Equal(Result.Failure<int, string>("missing"), await none.OrFailure("missing"));
        Assert.Equal(Result.Failure<int, string>("missing"), await none.OrFailure(() => "missing"));
        Assert.Equal(3, await none.ValueOr(3));
        Assert.Equal(3, await none.ValueOrElse(() => 3));
    }

    [Fact]
    public async Task TaskMatchActionOverloads_EachCase_InvokeSelectedAction()
    {
        var resultSuccess = 0;
        var resultFailure = string.Empty;
        var optionSome = 0;
        var optionNone = 0;

        await Task.FromResult(Result.Success<int, string>(2))
            .Match(value => resultSuccess = value, error => resultFailure = error);
        await Task.FromResult(Result.Failure<int, string>("error"))
            .Match(value => resultSuccess = value, error => resultFailure = error);
        await Task.FromResult(Option.Some(3))
            .Match(value => optionSome = value, () => optionNone++);
        await Task.FromResult(Option.None<int>())
            .Match(value => optionSome = value, () => optionNone++);

        Assert.Equal(2, resultSuccess);
        Assert.Equal("error", resultFailure);
        Assert.Equal(3, optionSome);
        Assert.Equal(1, optionNone);
    }

    [Fact]
    public async Task ResultAsyncExtensions_EveryOperation_ComposesSelectedCase()
    {
        var successTap = 0;
        var failureTap = string.Empty;
        var success = Result.Success<int, string>(2);
        var failure = Result.Failure<int, string>("error");

        Assert.Equal("2", await success.MatchAsync(value => Task.FromResult(value.ToString()), Task.FromResult));
        Assert.Equal("error", await failure.MatchAsync(value => Task.FromResult(value.ToString()), Task.FromResult));
        Assert.Equal(Result.Success<string, string>("2"), await success.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.Equal(Result.Failure<string, string>("error"), await failure.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.Equal(Result.Success<string, string>("2"), await success.BindAsync(value => Task.FromResult(Result.Success<string, string>(value.ToString()))));
        Assert.Equal(Result.Failure<string, string>("error"), await failure.BindAsync(value => Task.FromResult(Result.Success<string, string>(value.ToString()))));
        Assert.Equal(Result.Success<int, string>(2), await success.EnsureAsync(_ => Task.FromResult(true), () => "invalid"));
        Assert.Equal(Result.Failure<int, string>("invalid"), await success.EnsureAsync(_ => Task.FromResult(false), () => "invalid"));
        Assert.Equal(Result.Failure<int, string>("error"), await failure.EnsureAsync(_ => Task.FromResult(false), () => "invalid"));
        Assert.Equal(Result.Success<int, string>(2), await success.TapAsync(value =>
        {
            successTap = value;
            return Task.CompletedTask;
        }));
        Assert.Equal(Result.Failure<int, string>("error"), await failure.TapErrorAsync(error =>
        {
            failureTap = error;
            return Task.CompletedTask;
        }));
        Assert.Equal(Result.Success<int, string>(5), await failure.RecoverWithAsync(error => Task.FromResult(Result.Success<int, string>(error.Length))));
        Assert.Equal(Result.Success<int, string>(2), await success.RecoverWithAsync(error => Task.FromResult(Result.Success<int, string>(error.Length))));
        Assert.Equal(2, successTap);
        Assert.Equal("error", failureTap);
    }

    [Fact]
    public async Task ResultTaskAsyncExtensions_EveryOperation_ComposesSourceTask()
    {
        var success = Task.FromResult(Result.Success<int, string>(2));
        var failure = Task.FromResult(Result.Failure<int, string>("error"));

        Assert.Equal("2", await success.MatchAsync(value => Task.FromResult(value.ToString()), Task.FromResult));
        Assert.Equal(Result.Success<string, string>("2"), await success.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.Equal(Result.Success<string, string>("2"), await success.BindAsync(value => Task.FromResult(Result.Success<string, string>(value.ToString()))));
        Assert.Equal(Result.Success<int, string>(2), await success.EnsureAsync(_ => Task.FromResult(true), () => "invalid"));
        Assert.Equal(Result.Success<int, string>(2), await success.TapAsync(_ => Task.CompletedTask));
        Assert.Equal(Result.Failure<int, string>("error"), await failure.TapErrorAsync(_ => Task.CompletedTask));
        Assert.Equal(Result.Success<int, string>(5), await failure.RecoverWithAsync(error => Task.FromResult(Result.Success<int, string>(error.Length))));
    }

    [Fact]
    public async Task OptionAsyncExtensions_EveryOperation_ComposesSelectedCase()
    {
        var some = Option.Some(2);
        var none = Option.None<int>();

        Assert.Equal("2", await some.MatchAsync(value => Task.FromResult(value.ToString()), () => Task.FromResult("none")));
        Assert.Equal("none", await none.MatchAsync(value => Task.FromResult(value.ToString()), () => Task.FromResult("none")));
        Assert.Equal(Option.Some("2"), await some.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.True((await none.MapAsync(value => Task.FromResult(value.ToString()))).IsNone);
        Assert.Equal(Option.Some("2"), await some.BindAsync(value => Task.FromResult(Option.Some(value.ToString()))));
        Assert.True((await none.BindAsync(value => Task.FromResult(Option.Some(value.ToString())))).IsNone);
        Assert.Equal(Option.Some(3), await none.OrElseAsync(() => Task.FromResult(Option.Some(3))));
        Assert.Equal(Option.Some(2), await some.OrElseAsync(() => Task.FromResult(Option.Some(3))));
        Assert.Equal(Result.Failure<int, string>("missing"), await none.OrFailureAsync(() => Task.FromResult("missing")));
        Assert.Equal(Result.Success<int, string>(2), await some.OrFailureAsync(() => Task.FromResult("missing")));
        Assert.Equal(3, await none.ValueOrElseAsync(() => Task.FromResult(3)));
        Assert.Equal(2, await some.ValueOrElseAsync(() => Task.FromResult(3)));
    }

    [Fact]
    public async Task OptionTaskAsyncExtensions_EveryOperation_ComposesSourceTask()
    {
        var some = Task.FromResult(Option.Some(2));
        var none = Task.FromResult(Option.None<int>());

        Assert.Equal("2", await some.MatchAsync(value => Task.FromResult(value.ToString()), () => Task.FromResult("none")));
        Assert.Equal(Option.Some("2"), await some.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.Equal(Option.Some("2"), await some.BindAsync(value => Task.FromResult(Option.Some(value.ToString()))));
        Assert.Equal(Option.Some(3), await none.OrElseAsync(() => Task.FromResult(Option.Some(3))));
        Assert.Equal(Result.Failure<int, string>("missing"), await none.OrFailureAsync(() => Task.FromResult("missing")));
        Assert.Equal(3, await none.ValueOrElseAsync(() => Task.FromResult(3)));
    }

    [Fact]
    public async Task MatchAsyncActionOverloads_EachCase_InvokeSelectedTaskOnce()
    {
        var resultSuccess = 0;
        var resultFailure = string.Empty;
        var optionSome = 0;
        var optionNone = 0;

        await Result.Success<int, string>(2).MatchAsync(
            value =>
            {
                resultSuccess = value;
                return Task.CompletedTask;
            },
            error =>
            {
                resultFailure = error;
                return Task.CompletedTask;
            });
        await Task.FromResult(Result.Failure<int, string>("error")).MatchAsync(
            value =>
            {
                resultSuccess = value;
                return Task.CompletedTask;
            },
            error =>
            {
                resultFailure = error;
                return Task.CompletedTask;
            });
        await Option.Some(3).MatchAsync(
            value =>
            {
                optionSome = value;
                return Task.CompletedTask;
            },
            () =>
            {
                optionNone++;
                return Task.CompletedTask;
            });
        await Task.FromResult(Option.None<int>()).MatchAsync(
            value =>
            {
                optionSome = value;
                return Task.CompletedTask;
            },
            () =>
            {
                optionNone++;
                return Task.CompletedTask;
            });

        Assert.Equal(2, resultSuccess);
        Assert.Equal("error", resultFailure);
        Assert.Equal(3, optionSome);
        Assert.Equal(1, optionNone);
    }

    [Fact]
    public async Task AsyncExtensions_UnselectedDelegates_AreNeverInvoked()
    {
        var resultInvocations = 0;
        var optionInvocations = 0;
        var failure = Result.Failure<int, string>("error");
        var none = Option.None<int>();

        await failure.MapAsync(value =>
        {
            resultInvocations++;
            return Task.FromResult(value.ToString());
        });
        await failure.BindAsync(value =>
        {
            resultInvocations++;
            return Task.FromResult(Result.Success<string, string>(value.ToString()));
        });
        await failure.EnsureAsync(value =>
        {
            resultInvocations++;
            return Task.FromResult(value > 0);
        }, () => "invalid");
        await failure.TapAsync(value =>
        {
            resultInvocations++;
            return Task.CompletedTask;
        });
        await none.MapAsync(value =>
        {
            optionInvocations++;
            return Task.FromResult(value.ToString());
        });
        await none.BindAsync(value =>
        {
            optionInvocations++;
            return Task.FromResult(Option.Some(value.ToString()));
        });

        Assert.Equal(0, resultInvocations);
        Assert.Equal(0, optionInvocations);
    }

    [Fact]
    public async Task FaultedSourceTasks_PropagateOriginalExceptions()
    {
        var expected = new TestException();
        var resultSource = Task.FromException<Result<int, string>>(expected);
        var optionSource = Task.FromException<Option<int>>(expected);

        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() => resultSource.Map(value => value)));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() => resultSource.MapAsync(value => Task.FromResult(value))));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() => optionSource.Map(value => value)));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() => optionSource.MapAsync(value => Task.FromResult(value))));
    }

    [Fact]
    public async Task CancelledSourceTasks_RemainCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var resultSource = Task.FromCanceled<Result<int, string>>(cancellation.Token);
        var optionSource = Task.FromCanceled<Option<int>>(cancellation.Token);

        var resultTask = resultSource.Map(value => value);
        var optionTask = optionSource.Map(value => value);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => optionTask);
        Assert.True(resultTask.IsCanceled);
        Assert.True(optionTask.IsCanceled);
    }

    [Fact]
    public async Task AsyncDelegate_ThrownAndFaultedExceptions_PropagateUnchanged()
    {
        var thrown = new TestException();
        var faulted = new TestException();
        Func<int, Task<string>> throwing = _ => throw thrown;
        Func<int, Task<string>> faulting = _ => Task.FromException<string>(faulted);

        Assert.Same(thrown, await Assert.ThrowsAsync<TestException>(() => Result.Success<int, string>(1).MapAsync(throwing)));
        Assert.Same(faulted, await Assert.ThrowsAsync<TestException>(() => Result.Success<int, string>(1).MapAsync(faulting)));
        Assert.Same(thrown, await Assert.ThrowsAsync<TestException>(() => Option.Some(1).MapAsync(throwing)));
        Assert.Same(faulted, await Assert.ThrowsAsync<TestException>(() => Option.Some(1).MapAsync(faulting)));
    }

    [Fact]
    public async Task AsyncDelegate_CancelledTasks_RemainCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<int, Task<string>> cancelled = _ => Task.FromCanceled<string>(cancellation.Token);

        var resultTask = Result.Success<int, string>(1).MapAsync(cancelled);
        var optionTask = Option.Some(1).MapAsync(cancelled);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => optionTask);
        Assert.True(resultTask.IsCanceled);
        Assert.True(optionTask.IsCanceled);
    }

    [Fact]
    public async Task AsyncDelegate_NullTasks_ThrowArgumentNullException()
    {
        Func<int, Task<string>> nullTask = _ => null!;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success<int, string>(1).MapAsync(nullTask));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Option.Some(1).MapAsync(nullTask));
    }

    [Fact]
    public async Task AsyncDelegate_NullPayloads_ThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success<int, string>(1).MapAsync(_ => Task.FromResult<string>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Option.Some(1).MapAsync(_ => Task.FromResult<string>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Option.None<int>().OrFailureAsync(() => Task.FromResult<string>(null!)));
    }

    [Fact]
    public async Task UninitializedResult_AsyncOperationsFailExplicitly()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = result.MapAsync(value => Task.FromResult(value));
        });
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(result).MapAsync(value => Task.FromResult(value)));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(result).Map(value => value));
    }

    private sealed class TestException : Exception;
}
