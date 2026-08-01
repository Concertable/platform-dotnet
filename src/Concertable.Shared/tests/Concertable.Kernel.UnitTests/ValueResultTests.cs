using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class ValueResultTests
{
    [Fact]
    public void FactoriesPropertiesAndTryGet_CreateSelectedCases()
    {
        var success = Result<int>.Success(42);
        var failure = Result<int>.Failure("error");

        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.True(success.TryGetValue(out var value));
        Assert.Equal(42, value);
        Assert.False(success.TryGetError(out _));
        Assert.True(failure.IsFailure);
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("error", error);
        Assert.False(failure.TryGetValue(out _));
        Assert.Equal(success, Result.Success(42));
        Assert.Equal(failure, Result.Failure<int>("error"));
        Assert.Throws<ArgumentException>(() => Result<int>.Failure(" "));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void MatchMapBindAndMapError_PreserveSelectedCase()
    {
        var success = Result.Success(42);
        var failure = Result.Failure<int>("error");

        Assert.Equal("42", success.Match(value => value.ToString(), error => error));
        Assert.Equal("error", failure.Match(value => value.ToString(), error => error));
        Assert.Equal(Result.Success("42"), success.Map(value => value.ToString()));
        Assert.Equal(Result.Failure<string>("error"), failure.Map(value => value.ToString()));
        Assert.Equal(Result.Success("42"), success.Bind(value => Result.Success(value.ToString())));
        Assert.Equal(Result.Failure<string>("error"), failure.Bind(value => Result.Success(value.ToString())));
        Assert.Equal(Result.Success(), success.Bind(_ => Result.Success()));
        Assert.Equal(Result.Failure("error"), failure.Bind(_ => Result.Success()));
        Assert.Equal(Result.Success<int, int>(42), success.MapError(error => error.Length));
        Assert.Equal(Result.Failure<int, int>(5), failure.MapError(error => error.Length));
    }

    [Fact]
    public void EnsureTapAndRecovery_InvokeOnlySelectedDelegates()
    {
        var values = new List<int>();
        var errors = new List<string>();
        var valid = Result.Success(42).Ensure(value => value > 0, () => "invalid");
        var invalid = Result.Success(0).Ensure(value => value > 0, () => "invalid");
        var success = Result.Success(42).Tap(values.Add).TapError(errors.Add);
        var failure = Result.Failure<int>("error").Tap(values.Add).TapError(errors.Add);
        var recovered = failure.Recover(error => error.Length);
        var recoveredWith = failure.RecoverWith(error => Result.Success(error.Length));

        Assert.Equal(Result.Success(42), valid);
        Assert.Equal(Result.Failure<int>("invalid"), invalid);
        Assert.Equal(Result.Success(42), success);
        Assert.Equal(Result.Failure<int>("error"), failure);
        Assert.Equal(Result.Success(5), recovered);
        Assert.Equal(Result.Success(5), recoveredWith);
        Assert.Equal([42], values);
        Assert.Equal(["error"], errors);
    }

    [Fact]
    public async Task TaskSourceSynchronousExtensions_EveryOperation_ComposesSourceTask()
    {
        var values = new List<int>();
        var errors = new List<string>();
        var success = Task.FromResult(Result.Success(2));
        var failure = Task.FromResult(Result.Failure<int>("error"));

        Assert.Equal("2", await success.Match(value => value.ToString(), error => error));
        await failure.Match(values.Add, errors.Add);
        Assert.Equal(Result.Success("2"), await success.Map(value => value.ToString()));
        Assert.Equal(Result.Success("2"), await success.Bind(value => Result.Success(value.ToString())));
        Assert.Equal(Result.Success(), await success.Bind(_ => Result.Success()));
        Assert.Equal(Result.Failure<int, int>(5), await failure.MapError(error => error.Length));
        Assert.Equal(Result.Failure<int>("invalid"), await success.Ensure(_ => false, () => "invalid"));
        Assert.Equal(Result.Success(2), await success.Tap(values.Add));
        Assert.Equal(Result.Failure<int>("error"), await failure.TapError(errors.Add));
        Assert.Equal(Result.Success(5), await failure.Recover(error => error.Length));
        Assert.Equal(
            Result.Success(5),
            await failure.RecoverWith(error => Result.Success(error.Length)));
        Assert.Equal([2], values);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public async Task AsyncDelegateExtensions_DirectAndTaskSources_ComposeEveryOperation()
    {
        var values = new List<int>();
        var errors = new List<string>();
        var success = Result.Success(2);
        var failure = Result.Failure<int>("error");

        Assert.Equal(
            "2",
            await success.MatchAsync(
                value => Task.FromResult(value.ToString()),
                error => Task.FromResult(error)));
        await failure.MatchAsync(
            value =>
            {
                values.Add(value);
                return Task.CompletedTask;
            },
            error =>
            {
                errors.Add(error);
                return Task.CompletedTask;
            });
        Assert.Equal(Result.Success("2"), await success.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.Equal(
            Result.Success("2"),
            await success.BindAsync(value => Task.FromResult(Result.Success(value.ToString()))));
        Assert.Equal(Result.Success(2), await success.EnsureAsync(_ => Task.FromResult(true), () => "invalid"));
        Assert.Equal(Result.Success(2), await success.TapAsync(value =>
        {
            values.Add(value);
            return Task.CompletedTask;
        }));
        Assert.Equal(Result.Failure<int>("error"), await failure.TapErrorAsync(error =>
        {
            errors.Add(error);
            return Task.CompletedTask;
        }));
        Assert.Equal(
            Result.Success(5),
            await failure.RecoverWithAsync(
                error => Task.FromResult(Result.Success(error.Length))));

        var successTask = Task.FromResult(success);
        var failureTask = Task.FromResult(failure);
        Assert.Equal("2", await successTask.MatchAsync(
            value => Task.FromResult(value.ToString()),
            error => Task.FromResult(error)));
        await failureTask.MatchAsync(_ => Task.CompletedTask, _ => Task.CompletedTask);
        Assert.Equal(Result.Success("2"), await successTask.MapAsync(value => Task.FromResult(value.ToString())));
        Assert.Equal(
            Result.Success("2"),
            await successTask.BindAsync(value => Task.FromResult(Result.Success(value.ToString()))));
        Assert.Equal(
            Result.Success(2),
            await successTask.EnsureAsync(_ => Task.FromResult(true), () => "invalid"));
        Assert.Equal(Result.Success(2), await successTask.TapAsync(_ => Task.CompletedTask));
        Assert.Equal(Result.Failure<int>("error"), await failureTask.TapErrorAsync(_ => Task.CompletedTask));
        Assert.Equal(
            Result.Success(5),
            await failureTask.RecoverWithAsync(
                error => Task.FromResult(Result.Success(error.Length))));
        Assert.Equal([2], values);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public async Task AsyncExtensions_UnselectedDelegates_AreNotInvoked()
    {
        var invocations = 0;
        var failure = Result.Failure<int>("error");
        var success = Result.Success(2);

        await failure.MapAsync(value =>
        {
            invocations++;
            return Task.FromResult(value.ToString());
        });
        await failure.BindAsync(value =>
        {
            invocations++;
            return Task.FromResult(Result.Success(value.ToString()));
        });
        await failure.EnsureAsync(value =>
        {
            invocations++;
            return Task.FromResult(value > 0);
        }, () => "invalid");
        await failure.TapAsync(_ =>
        {
            invocations++;
            return Task.CompletedTask;
        });
        await success.TapErrorAsync(_ =>
        {
            invocations++;
            return Task.CompletedTask;
        });
        await success.RecoverWithAsync(_ =>
        {
            invocations++;
            return Task.FromResult(Result.Success(0));
        });

        Assert.Equal(0, invocations);
    }

    [Fact]
    public async Task AsyncExtensions_FaultCancellationNullTasksAndDefaults_Propagate()
    {
        var sourceException = new TestException();
        var delegateException = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var faultedSource = Task.FromException<Result<int>>(sourceException);
        var cancelledSource = Task.FromCanceled<Result<int>>(cancellation.Token);
        Func<int, Task<string>> throwing = _ => throw delegateException;
        Func<int, Task<string>> faulting = _ => Task.FromException<string>(delegateException);
        Func<int, Task<string>> cancelled = _ => Task.FromCanceled<string>(cancellation.Token);

        Assert.Same(
            sourceException,
            await Assert.ThrowsAsync<TestException>(() => faultedSource.Map(value => value)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledSource.MapAsync(value => Task.FromResult(value)));
        Assert.Same(
            delegateException,
            await Assert.ThrowsAsync<TestException>(() => Result.Success(1).MapAsync(throwing)));
        Assert.Same(
            delegateException,
            await Assert.ThrowsAsync<TestException>(() => Result.Success(1).MapAsync(faulting)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Result.Success(1).MapAsync(cancelled));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success(1).MapAsync<int, string>(_ => null!));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => default(Result<int>).MatchAsync(
                value => Task.FromResult(value),
                _ => Task.FromResult(0)));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(default(Result<int>)).MapAsync(value => Task.FromResult(value)));
    }

    [Fact]
    public void EqualityHashingFormattingLawsAndSurface_AreStable()
    {
        Func<int, Result<int>> first = value => Result.Success(value + 1);
        Func<int, Result<string>> second = value => Result.Success($"{value}!");
        var success = Result.Success(42);
        var failure = Result.Failure<int>("error");
        var sameFailure = Result.Failure<int>("error");
        var type = typeof(Result<int>);

        Assert.Equal(first(42), Result.Success(42).Bind(first));
        foreach (var result in new[] { success, failure })
        {
            Assert.Equal(result, result.Bind(Result.Success));
            Assert.Equal(
                result.Bind(first).Bind(second),
                result.Bind(value => first(value).Bind(second)));
        }

        Assert.Equal(failure, sameFailure);
        Assert.Equal(failure.GetHashCode(), sameFailure.GetHashCode());
        Assert.True(failure == sameFailure);
        Assert.True(success != failure);
        Assert.Equal("Success(42)", success.ToString());
        Assert.Equal("Failure(error)", failure.ToString());
        Assert.Equal("Uninitialized", default(Result<int>).ToString());
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Value" or "Error");
        Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == "op_Implicit");
    }

    private sealed class TestException : Exception;
}
