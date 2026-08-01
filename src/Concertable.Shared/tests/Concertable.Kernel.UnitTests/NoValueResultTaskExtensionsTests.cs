using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class NoValueResultTaskExtensionsTests
{
    [Fact]
    public async Task StatusTaskSource_SynchronousCombinators_PreserveError()
    {
        var errors = new List<string>();
        var matched = await Task.FromResult(Result.Failure("error"))
            .Match(() => 0, error => error.Length);
        var bound = await Task.FromResult(Result.Failure("error"))
            .Bind(() => UnitResult.Success<int>(), error => error.Length);
        var mapped = await Task.FromResult(Result.Failure("error"))
            .MapError(error => error.Length);
        var tapped = await Task.FromResult(Result.Failure("error")).TapError(errors.Add);
        var recovered = await Task.FromResult(Result.Failure("error")).Recover(errors.Add);
        var recoveredWith = await Task.FromResult(Result.Failure("error"))
            .RecoverWith(_ => Result.Success());

        Assert.Equal(5, matched);
        Assert.Equal(UnitResult.Failure(5), bound);
        Assert.Equal(UnitResult.Failure(5), mapped);
        Assert.Equal(Result.Failure("error"), tapped);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(Result.Success(), recoveredWith);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public async Task UnitTaskSource_SynchronousCombinators_PreserveTypedError()
    {
        var errors = new List<string>();
        var matched = await Task.FromResult(UnitResult.Failure("error"))
            .Match(() => 0, error => error.Length);
        var bound = await Task.FromResult(UnitResult.Success<string>())
            .Bind(() => Result.Success<int, string>(42));
        var mapped = await Task.FromResult(UnitResult.Failure("error"))
            .MapError(error => error.Length);
        var tapped = await Task.FromResult(UnitResult.Failure("error"))
            .TapError(errors.Add);
        var recovered = await Task.FromResult(UnitResult.Failure("error"))
            .Recover(errors.Add);

        Assert.Equal(5, matched);
        Assert.Equal(Result.Success<int, string>(42), bound);
        Assert.Equal(UnitResult.Failure(5), mapped);
        Assert.Equal(UnitResult.Failure("error"), tapped);
        Assert.Equal(UnitResult.Success<string>(), recovered);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public async Task StatusAsyncDelegates_InvokeOnlySelectedBranches()
    {
        var errors = new List<string>();
        var matched = await Result.Failure("error").MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length));
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
        Assert.Equal(["error"], errors);
    }

    [Fact]
    public async Task UnitAsyncDelegates_InvokeOnlySelectedBranches()
    {
        var errors = new List<string>();
        var matched = await UnitResult.Failure("error").MatchAsync(
            () => Task.FromResult(0),
            error => Task.FromResult(error.Length));
        var bound = await UnitResult.Success<string>().BindAsync(
            () => Task.FromResult(Result.Success<int, string>(42)));
        var tapped = await UnitResult.Failure("error").TapErrorAsync(error =>
        {
            errors.Add(error);
            return Task.CompletedTask;
        });
        var recovered = await UnitResult.Failure("error").RecoverWithAsync(
            _ => Task.FromResult(UnitResult.Success<string>()));

        Assert.Equal(5, matched);
        Assert.Equal(Result.Success<int, string>(42), bound);
        Assert.Equal(UnitResult.Failure("error"), tapped);
        Assert.Equal(UnitResult.Success<string>(), recovered);
        Assert.Equal(["error"], errors);
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
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Result.Success().BindAsync(() => Task.FromResult(default(Result))));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => default(Result).MatchAsync(
                () => Task.FromResult(1),
                _ => Task.FromResult(0)));
    }

    private sealed class TestException : Exception;
}
