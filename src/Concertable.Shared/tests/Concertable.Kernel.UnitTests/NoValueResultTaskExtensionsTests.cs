using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class NoValueResultTaskExtensionsTests
{
    [Fact]
    public async Task StatusTaskSource_SynchronousCombinators_PreserveError()
    {
        var successes = 0;
        var errors = new List<string>();
        var success = Task.FromResult(Result.Success());
        var failure = Task.FromResult(Result.Failure("error"));
        var matched = await failure
            .Match(() => 0, error => error.Length);
        await failure.Match(() => successes++, errors.Add);
        var statusBound = await failure.Bind(Result.Success);
        var bound = await failure
            .Bind(() => UnitResult.Success<int>(), error => error.Length);
        var valueBound = await failure.Bind(
            () => Result.Success<int, int>(42),
            error => error.Length);
        var mapped = await failure
            .MapError(error => error.Length);
        var tappedSuccess = await success.Tap(() => successes++);
        var tapped = await failure.TapError(errors.Add);
        var recovered = await failure.Recover(errors.Add);
        var recoveredWith = await failure
            .RecoverWith(_ => Result.Success());

        Assert.Equal(5, matched);
        Assert.Equal(Result.Failure("error"), statusBound);
        Assert.Equal(UnitResult.Failure(5), bound);
        Assert.Equal(Result.Failure<int, int>(5), valueBound);
        Assert.Equal(UnitResult.Failure(5), mapped);
        Assert.Equal(Result.Success(), tappedSuccess);
        Assert.Equal(Result.Failure("error"), tapped);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(Result.Success(), recoveredWith);
        Assert.Equal(1, successes);
        Assert.Equal(["error", "error", "error"], errors);
    }

    [Fact]
    public async Task UnitTaskSource_SynchronousCombinators_PreserveTypedError()
    {
        var successes = 0;
        var errors = new List<string>();
        var success = Task.FromResult(UnitResult.Success<string>());
        var failure = Task.FromResult(UnitResult.Failure("error"));
        var matched = await failure
            .Match(() => 0, error => error.Length);
        await failure.Match(() => successes++, errors.Add);
        var unitBound = await success.Bind(() => UnitResult.Success<string>());
        var bound = await success
            .Bind(() => Result.Success<int, string>(42));
        var mapped = await failure
            .MapError(error => error.Length);
        var tappedSuccess = await success.Tap(() => successes++);
        var tapped = await failure.TapError(errors.Add);
        var recovered = await failure.Recover(errors.Add);
        var recoveredWith = await failure.RecoverWith(_ => UnitResult.Success<string>());

        Assert.Equal(5, matched);
        Assert.Equal(UnitResult.Success<string>(), unitBound);
        Assert.Equal(Result.Success<int, string>(42), bound);
        Assert.Equal(UnitResult.Failure(5), mapped);
        Assert.Equal(UnitResult.Success<string>(), tappedSuccess);
        Assert.Equal(UnitResult.Failure("error"), tapped);
        Assert.Equal(UnitResult.Success<string>(), recovered);
        Assert.Equal(UnitResult.Success<string>(), recoveredWith);
        Assert.Equal(1, successes);
        Assert.Equal(["error", "error", "error"], errors);
    }

    [Fact]
    public async Task StatusAsyncDelegates_InvokeOnlySelectedBranches()
    {
        var errors = new List<string>();
        var matched = await Result.Failure("error").MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length));
        await Result.Failure("error").MatchAsync(
            () => Task.CompletedTask,
            error =>
            {
                errors.Add(error);
                return Task.CompletedTask;
            });
        var bound = await Result.Success().BindAsync(
            () => Task.FromResult(Result.Failure("bound")));
        var tappedSuccess = await Result.Success().TapAsync(() => Task.CompletedTask);
        var tappedFailure = await Result.Failure("error").TapErrorAsync(error =>
        {
            errors.Add(error);
            return Task.CompletedTask;
        });
        var recovered = await Result.Failure("error").RecoverWithAsync(
            _ => Task.FromResult(Result.Success()));

        Assert.Equal(5, matched);
        Assert.Equal(Result.Failure("bound"), bound);
        Assert.Equal(Result.Success(), tappedSuccess);
        Assert.Equal(Result.Failure("error"), tappedFailure);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public async Task UnitAsyncDelegates_InvokeOnlySelectedBranches()
    {
        var errors = new List<string>();
        var matched = await UnitResult.Failure("error").MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length));
        await UnitResult.Failure("error").MatchAsync(
            () => Task.CompletedTask,
            error =>
            {
                errors.Add(error);
                return Task.CompletedTask;
            });
        var unitBound = await UnitResult.Success<string>().BindAsync(
            () => Task.FromResult(UnitResult.Success<string>()));
        var bound = await UnitResult.Success<string>().BindAsync(
            () => Task.FromResult(Result.Success<int, string>(42)));
        var tappedSuccess = await UnitResult.Success<string>().TapAsync(() => Task.CompletedTask);
        var tapped = await UnitResult.Failure("error").TapErrorAsync(error =>
        {
            errors.Add(error);
            return Task.CompletedTask;
        });
        var recovered = await UnitResult.Failure("error").RecoverWithAsync(
            _ => Task.FromResult(UnitResult.Success<string>()));

        Assert.Equal(5, matched);
        Assert.Equal(UnitResult.Success<string>(), unitBound);
        Assert.Equal(Result.Success<int, string>(42), bound);
        Assert.Equal(UnitResult.Success<string>(), tappedSuccess);
        Assert.Equal(UnitResult.Failure("error"), tapped);
        Assert.Equal(UnitResult.Success<string>(), recovered);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public async Task TaskSourceAsyncDelegates_ComposeEveryOverload()
    {
        var statusSuccess = Task.FromResult(Result.Success());
        var statusFailure = Task.FromResult(Result.Failure("error"));
        var unitSuccess = Task.FromResult(UnitResult.Success<string>());
        var unitFailure = Task.FromResult(UnitResult.Failure("error"));

        Assert.Equal(5, await statusFailure.MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length)));
        await statusFailure.MatchAsync(() => Task.CompletedTask, _ => Task.CompletedTask);
        Assert.Equal(Result.Success(), await statusSuccess.BindAsync(() => Task.FromResult(Result.Success())));
        Assert.Equal(Result.Success(), await statusSuccess.TapAsync(() => Task.CompletedTask));
        Assert.Equal(Result.Failure("error"), await statusFailure.TapErrorAsync(_ => Task.CompletedTask));
        Assert.Equal(
            Result.Success(),
            await statusFailure.RecoverWithAsync(_ => Task.FromResult(Result.Success())));

        Assert.Equal(5, await unitFailure.MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length)));
        await unitFailure.MatchAsync(() => Task.CompletedTask, _ => Task.CompletedTask);
        Assert.Equal(
            UnitResult.Success<string>(),
            await unitSuccess.BindAsync(() => Task.FromResult(UnitResult.Success<string>())));
        Assert.Equal(
            Result.Success<int, string>(42),
            await unitSuccess.BindAsync(() => Task.FromResult(Result.Success<int, string>(42))));
        Assert.Equal(UnitResult.Success<string>(), await unitSuccess.TapAsync(() => Task.CompletedTask));
        Assert.Equal(
            UnitResult.Failure("error"),
            await unitFailure.TapErrorAsync(_ => Task.CompletedTask));
        Assert.Equal(
            UnitResult.Success<string>(),
            await unitFailure.RecoverWithAsync(_ => Task.FromResult(UnitResult.Success<string>())));
    }

    [Fact]
    public async Task AsyncExtensions_UnselectedDelegates_AreNotInvoked()
    {
        var invocations = 0;

        await Result.Failure("error").TapAsync(() =>
        {
            invocations++;
            return Task.CompletedTask;
        });
        await Result.Success().TapErrorAsync(_ =>
        {
            invocations++;
            return Task.CompletedTask;
        });
        await Result.Success().RecoverWithAsync(_ =>
        {
            invocations++;
            return Task.FromResult(Result.Success());
        });
        await UnitResult.Failure("error").TapAsync(() =>
        {
            invocations++;
            return Task.CompletedTask;
        });
        await UnitResult.Success<string>().TapErrorAsync(_ =>
        {
            invocations++;
            return Task.CompletedTask;
        });
        await UnitResult.Success<string>().RecoverWithAsync(_ =>
        {
            invocations++;
            return Task.FromResult(UnitResult.Success<string>());
        });

        Assert.Equal(0, invocations);
    }

    [Fact]
    public async Task FaultCancellationNullTasksAndDefaults_Propagate()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(
            expected,
            await Assert.ThrowsAsync<TestException>(
                () => Task.FromException<Result>(expected).Tap(() => { })));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.FromCanceled<UnitResult<string>>(cancellation.Token).TapError(_ => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success().BindAsync(() => null!));
        Assert.Same(
            expected,
            await Assert.ThrowsAsync<TestException>(
                () => Result.Success().TapAsync(() => Task.FromException(expected))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => UnitResult.Failure("error").TapErrorAsync(
                _ => Task.FromCanceled(cancellation.Token)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => UnitResult.Success<string>().BindAsync(() => null!));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Result.Success().BindAsync(() => Task.FromResult(default(Result))));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => default(Result).MatchAsync(
                () => Task.FromResult(1),
                _ => Task.FromResult(0)));
    }

    private sealed class TestException : Exception;
}
