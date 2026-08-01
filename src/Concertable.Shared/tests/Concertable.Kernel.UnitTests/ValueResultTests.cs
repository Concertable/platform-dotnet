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
    public async Task TaskExtensions_PreserveSemanticsAndAsyncFailures()
    {
        var mapped = await Task.FromResult(Result.Success(42)).Map(value => value.ToString());
        var asyncMapped = await Result.Success(42).MapAsync(value => Task.FromResult(value.ToString()));
        var bound = await Task.FromResult(Result.Success(42))
            .BindAsync(value => Task.FromResult(Result.Success(value.ToString())));
        var matched = await Result.Failure<int>("error").MatchAsync(
            value => Task.FromResult(value),
            error => Task.FromResult(error.Length));
        var tapped = await Result.Failure<int>("error")
            .TapErrorAsync(_ => Task.CompletedTask);
        var recovered = await Result.Failure<int>("error")
            .RecoverWithAsync(error => Task.FromResult(Result.Success(error.Length)));

        Assert.Equal(Result.Success("42"), mapped);
        Assert.Equal(Result.Success("42"), asyncMapped);
        Assert.Equal(Result.Success("42"), bound);
        Assert.Equal(5, matched);
        Assert.Equal(Result.Failure<int>("error"), tapped);
        Assert.Equal(Result.Success(5), recovered);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Result.Success(42).BindAsync<int, string>(_ => null!));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => default(Result<int>).MatchAsync(
                value => Task.FromResult(value),
                _ => Task.FromResult(0)));
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
}
