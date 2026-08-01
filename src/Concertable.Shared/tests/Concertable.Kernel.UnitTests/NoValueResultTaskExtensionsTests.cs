using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class NoValueResultTaskExtensionsTests
{
    [Fact]
    public async Task StatusTaskSource_SynchronousCombinators_PreserveSemantics()
    {
        var successes = 0;
        var failures = 0;
        var matched = await Task.FromResult(Result.Success()).Match(
            () => "success",
            () => "failure");
        await Task.FromResult(Result.Failure()).Match(
            () => successes++,
            () => failures++);
        var bound = await Task.FromResult(Result.Success())
            .Bind(() => Result.Success<string>(), () => "mapped");
        var mapped = await Task.FromResult(Result.Failure())
            .MapError(() => "mapped");
        var tapped = await Task.FromResult(Result.Success()).Tap(() => successes++);
        var tappedFailure = await Task.FromResult(Result.Failure()).TapFailure(() => failures++);
        var recovered = await Task.FromResult(Result.Failure()).Recover(() => successes++);
        var recoveredWith = await Task.FromResult(Result.Failure())
            .RecoverWith(Result.Success);

        Assert.Equal("success", matched);
        Assert.Equal(Result.Success<string>(), bound);
        Assert.Equal(Result.Failure<string>("mapped"), mapped);
        Assert.Equal(Result.Success(), tapped);
        Assert.Equal(Result.Failure(), tappedFailure);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(Result.Success(), recoveredWith);
        Assert.Equal(2, successes);
        Assert.Equal(2, failures);
    }

    [Fact]
    public async Task ErrorTaskSource_SynchronousCombinators_PreserveSemantics()
    {
        var successes = 0;
        var failures = 0;
        var matched = await Task.FromResult(Result.Failure<string>("error")).Match(
            () => 0,
            error => error.Length);
        await Task.FromResult(Result.Success<string>()).Match(
            () => successes++,
            _ => failures++);
        var bound = await Task.FromResult(Result.Success<string>())
            .Bind(() => Result.Success<int, string>(42));
        var mapped = await Task.FromResult(Result.Failure<string>("error"))
            .MapError(error => error.Length);
        var tapped = await Task.FromResult(Result.Success<string>()).Tap(() => successes++);
        var tappedError = await Task.FromResult(Result.Failure<string>("error"))
            .TapError(_ => failures++);
        var recovered = await Task.FromResult(Result.Failure<string>("error"))
            .Recover(_ => successes++);
        var recoveredWith = await Task.FromResult(Result.Failure<string>("error"))
            .RecoverWith(_ => Result.Success<string>());

        Assert.Equal(5, matched);
        Assert.Equal(Result.Success<int, string>(42), bound);
        Assert.Equal(Result.Failure<int>(5), mapped);
        Assert.Equal(Result.Success<string>(), tapped);
        Assert.Equal(Result.Failure<string>("error"), tappedError);
        Assert.Equal(Result.Success<string>(), recovered);
        Assert.Equal(Result.Success<string>(), recoveredWith);
        Assert.Equal(3, successes);
        Assert.Equal(1, failures);
    }

    [Fact]
    public async Task StatusAsyncDelegates_InvokeOnlySelectedBranches()
    {
        var successes = 0;
        var failures = 0;
        var matched = await Result.Success().MatchAsync(
            () => Task.FromResult("success"),
            () => Task.FromResult("failure"));
        await Result.Failure().MatchAsync(
            () =>
            {
                successes++;
                return Task.CompletedTask;
            },
            () =>
            {
                failures++;
                return Task.CompletedTask;
            });
        var bound = await Result.Success().BindAsync(
            () => Task.FromResult(Result.Success()));
        var tapped = await Result.Success().TapAsync(() =>
        {
            successes++;
            return Task.CompletedTask;
        });
        var tappedFailure = await Result.Failure().TapFailureAsync(() =>
        {
            failures++;
            return Task.CompletedTask;
        });
        var recovered = await Result.Failure().RecoverWithAsync(
            () => Task.FromResult(Result.Success()));

        Assert.Equal("success", matched);
        Assert.Equal(Result.Success(), bound);
        Assert.Equal(Result.Success(), tapped);
        Assert.Equal(Result.Failure(), tappedFailure);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(1, successes);
        Assert.Equal(2, failures);
    }

    [Fact]
    public async Task ErrorAsyncDelegates_InvokeOnlySelectedBranches()
    {
        var successes = 0;
        var failures = 0;
        var matched = await Result.Failure<string>("error").MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length));
        await Result.Success<string>().MatchAsync(
            () =>
            {
                successes++;
                return Task.CompletedTask;
            },
            _ =>
            {
                failures++;
                return Task.CompletedTask;
            });
        var bound = await Result.Success<string>().BindAsync(
            () => Task.FromResult(Result.Success<int, string>(42)));
        var tapped = await Result.Success<string>().TapAsync(() =>
        {
            successes++;
            return Task.CompletedTask;
        });
        var tappedError = await Result.Failure<string>("error").TapErrorAsync(error =>
        {
            failures += error.Length;
            return Task.CompletedTask;
        });
        var recovered = await Result.Failure<string>("error").RecoverWithAsync(
            _ => Task.FromResult(Result.Success<string>()));

        Assert.Equal(5, matched);
        Assert.Equal(Result.Success<int, string>(42), bound);
        Assert.Equal(Result.Success<string>(), tapped);
        Assert.Equal(Result.Failure<string>("error"), tappedError);
        Assert.Equal(Result.Success<string>(), recovered);
        Assert.Equal(2, successes);
        Assert.Equal(5, failures);
    }

    [Fact]
    public async Task TaskSourceOverloads_AsyncDelegates_PreserveSemantics()
    {
        var status = await Task.FromResult(Result.Success()).BindAsync(
            () => Task.FromResult(Result.Failure()));
        var typed = await Task.FromResult(Result.Success<string>()).BindAsync(
            () => Task.FromResult(Result.Success<int, string>(42)));
        var valueToNoValue = await Task.FromResult(Result.Success<int, string>(42)).Bind(
            _ => Result.Success<string>());
        var asyncValueToNoValue = await Result.Success<int, string>(42).BindAsync(
            _ => Task.FromResult(Result.Success<string>()));

        Assert.Equal(Result.Failure(), status);
        Assert.Equal(Result.Success<int, string>(42), typed);
        Assert.Equal(Result.Success<string>(), valueToNoValue);
        Assert.Equal(Result.Success<string>(), asyncValueToNoValue);
    }

    [Fact]
    public async Task FaultedAndCancelledSources_Propagate()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(
            expected,
            await Assert.ThrowsAsync<TestException>(
                () => Task.FromException<Result>(expected).Tap(() => { })));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.FromCanceled<Result<string>>(cancellation.Token)
                .TapError(_ => { }));
    }

    [Fact]
    public async Task SelectedAsyncDelegateFaultOrCancellation_Propagates()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(
            expected,
            await Assert.ThrowsAsync<TestException>(
                () => Result.Success().TapAsync(
                    () => Task.FromException(expected))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Result.Failure<string>("error").TapErrorAsync(
                _ => Task.FromCanceled(cancellation.Token)));
    }

    [Fact]
    public async Task UnselectedAsyncDelegates_AreNotInvoked()
    {
        var status = await Result.Failure().TapAsync(
            () => throw new TestException());
        var error = await Result.Success<string>().TapErrorAsync(
            _ => throw new TestException());

        Assert.Equal(Result.Failure(), status);
        Assert.Equal(Result.Success<string>(), error);
    }

    [Fact]
    public async Task NullTaskAndUninitializedResults_Throw()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => default(Result).MatchAsync(
                () => Task.FromResult(1),
                () => Task.FromResult(0)));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => default(Result<string>).TapAsync(() => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success().BindAsync(() => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success<string>().BindAsync(() => null!));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Result.Success().BindAsync(() => Task.FromResult(default(Result))));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Result.Success<string>().BindAsync(
                () => Task.FromResult(default(Result<string>))));
    }

    private sealed class TestException : Exception;
}
