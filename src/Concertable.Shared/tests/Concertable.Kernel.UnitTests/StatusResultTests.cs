using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class StatusResultTests
{
    [Fact]
    public void FactoriesAndCaseProperties_CreateSelectedCases()
    {
        var success = Result.Success();
        var failure = Result.Failure("error");

        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.False(failure.IsSuccess);
        Assert.True(failure.IsFailure);
        Assert.False(success.TryGetError(out _));
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("error", error);
        Assert.Throws<ArgumentException>(() => Result.Failure(" "));
    }

    [Fact]
    public void Match_EachCase_InvokesSelectedDelegateOnce()
    {
        var successes = 0;
        var errors = new List<string>();

        var successValue = Result.Success().Match(() => ++successes, error => error.Length);
        Result.Failure("error").Match(() => successes++, errors.Add);

        Assert.Equal(1, successValue);
        Assert.Equal(1, successes);
        Assert.Equal(["error"], errors);
    }

    [Fact]
    public void Bind_EachTargetShape_ShortCircuitsAndMapsFailure()
    {
        var statusSuccess = Result.Success().Bind(Result.Success);
        var statusFailure = Result.Failure("error").Bind(Result.Success);
        var valueSuccess = Result.Success().Bind(() => Result.Success(42));
        var valueFailure = Result.Failure("error").Bind(() => Result.Success(42));
        var unitSuccess = Result.Success().Bind(
            () => UnitResult.Success<int>(),
            error => error.Length);
        var unitFailure = Result.Failure("error").Bind(
            () => UnitResult.Success<int>(),
            error => error.Length);
        var typedSuccess = Result.Success().Bind(
            () => Result.Success<int, int>(42),
            error => error.Length);
        var typedFailure = Result.Failure("error").Bind(
            () => Result.Success<int, int>(42),
            error => error.Length);

        Assert.Equal(Result.Success(), statusSuccess);
        Assert.Equal(Result.Failure("error"), statusFailure);
        Assert.Equal(Result.Success(42), valueSuccess);
        Assert.Equal(Result.Failure<int>("error"), valueFailure);
        Assert.Equal(UnitResult.Success<int>(), unitSuccess);
        Assert.Equal(UnitResult.Failure(5), unitFailure);
        Assert.Equal(Result.Success<int, int>(42), typedSuccess);
        Assert.Equal(Result.Failure<int, int>(5), typedFailure);
    }

    [Fact]
    public void MapErrorTapAndRecovery_InvokeOnlySelectedDelegates()
    {
        var successes = 0;
        var errors = new List<string>();
        var mappedSuccess = Result.Success().MapError(error => error.Length);
        var mappedFailure = Result.Failure("error").MapError(error => error.Length);
        var success = Result.Success().Tap(() => successes++).TapError(errors.Add);
        var failure = Result.Failure("error").Tap(() => successes++).TapError(errors.Add);
        var recovered = failure.Recover(errors.Add);
        var recoveredWith = failure.RecoverWith(_ => Result.Success());

        Assert.Equal(UnitResult.Success<int>(), mappedSuccess);
        Assert.Equal(UnitResult.Failure(5), mappedFailure);
        Assert.Equal(Result.Success(), success);
        Assert.Equal(Result.Failure("error"), failure);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(Result.Success(), recoveredWith);
        Assert.Equal(1, successes);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public void InvalidDelegatesAndUninitializedResults_AreRejected()
    {
        var result = Result.Success();
        Assert.Throws<ArgumentNullException>(() => result.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => 0, null!));
        Assert.Throws<ArgumentNullException>(() => result.Bind(null!));
        Assert.Throws<ArgumentNullException>(() => result.MapError<int>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => result.TapError(null!));
        Assert.Throws<ArgumentNullException>(() => result.Recover(null!));
        Assert.Throws<ArgumentNullException>(() => result.RecoverWith(null!));
        Assert.Throws<InvalidOperationException>(() => result.Bind(() => default(Result)));
        Assert.Throws<InvalidOperationException>(
            () => Result.Failure("error").RecoverWith(_ => default));

        var uninitialized = default(Result);
        Assert.Throws<InvalidOperationException>(() => _ = uninitialized.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => uninitialized.TryGetError(out _));
    }

    [Fact]
    public void EqualityHashingFormattingAndSurface_IncludeFailurePayload()
    {
        var success = Result.Success();
        var failure = Result.Failure("error");
        var sameFailure = Result.Failure("error");
        var otherFailure = Result.Failure("other");
        var type = typeof(Result);

        Assert.Equal(failure, sameFailure);
        Assert.Equal(failure.GetHashCode(), sameFailure.GetHashCode());
        Assert.NotEqual(failure, otherFailure);
        Assert.True(failure == sameFailure);
        Assert.True(success != failure);
        Assert.Equal("Success", success.ToString());
        Assert.Equal("Failure(error)", failure.ToString());
        Assert.Equal("Uninitialized", default(Result).ToString());
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Value" or "Error");
        Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == "op_Implicit");
    }
}
