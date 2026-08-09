using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class UnitResultTests
{
    [Fact]
    public void FactoriesPropertiesAndTryGet_CreateSelectedCases()
    {
        var success = UnitResult<string>.Success();
        var failure = UnitResult<string>.Failure("error");

        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.False(success.TryGetError(out _));
        Assert.True(failure.IsFailure);
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("error", error);
        Assert.Equal(success, UnitResult.Success<string>());
        Assert.Equal(failure, UnitResult.Failure("error"));
        Assert.Throws<ArgumentNullException>(() => UnitResult<string>.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => UnitResult.Failure<string>(null!));
    }

    [Fact]
    public void MatchAndBind_EachCase_InvokesOnlySelectedDelegate()
    {
        var successes = 0;
        var errors = new List<string>();
        var success = UnitResult.Success<string>();
        var failure = UnitResult.Failure("error");

        success.Match(() => successes++, errors.Add);
        failure.Match(() => successes++, errors.Add);
        var unitSuccess = success.Bind(() => UnitResult.Success<string>());
        var unitFailure = failure.Bind(() => UnitResult.Success<string>());
        var valueSuccess = success.Bind(() => Result.Success<int, string>(42));
        var valueFailure = failure.Bind(() => Result.Success<int, string>(42));
        var statusFailure = failure.Bind(Result.Success, error => error);
        var untypedValueFailure = failure.Bind(() => Result.Success(42), error => error);

        Assert.Equal(1, successes);
        Assert.Equal(["error"], errors);
        Assert.Equal(UnitResult.Success<string>(), unitSuccess);
        Assert.Equal(UnitResult.Failure("error"), unitFailure);
        Assert.Equal(Result.Success<int, string>(42), valueSuccess);
        Assert.Equal(Result.Failure<int, string>("error"), valueFailure);
        Assert.Equal(Result.Failure("error"), statusFailure);
        Assert.Equal(Result.Failure<int>("error"), untypedValueFailure);
    }

    [Fact]
    public void MapErrorTapAndRecovery_InvokeOnlySelectedDelegates()
    {
        var successes = 0;
        var errors = new List<string>();
        var mappedSuccess = UnitResult.Success<string>().MapError(error => error.Length);
        var mappedFailure = UnitResult.Failure("error").MapError(error => error.Length);
        var success = UnitResult.Success<string>().Tap(() => successes++).TapError(errors.Add);
        var failure = UnitResult.Failure("error").Tap(() => successes++).TapError(errors.Add);
        var recovered = failure.Recover(errors.Add);
        var recoveredWith = failure.RecoverWith(_ => UnitResult.Success<string>());

        Assert.Equal(UnitResult.Success<int>(), mappedSuccess);
        Assert.Equal(UnitResult.Failure(5), mappedFailure);
        Assert.Equal(UnitResult.Success<string>(), success);
        Assert.Equal(UnitResult.Failure("error"), failure);
        Assert.Equal(UnitResult.Success<string>(), recovered);
        Assert.Equal(UnitResult.Success<string>(), recoveredWith);
        Assert.Equal(1, successes);
        Assert.Equal(["error", "error"], errors);
    }

    [Fact]
    public void InvalidDelegatesAndUninitializedResults_AreRejected()
    {
        var result = UnitResult.Success<string>();
        Assert.Throws<ArgumentNullException>(() => result.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => 0, null!));
        Assert.Throws<ArgumentNullException>(() => result.Bind((Func<UnitResult<string>>)null!));
        Assert.Throws<ArgumentNullException>(() => result.MapError<int>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => result.TapError(null!));
        Assert.Throws<ArgumentNullException>(() => result.Recover(null!));
        Assert.Throws<ArgumentNullException>(() => result.RecoverWith(null!));
        Assert.Throws<InvalidOperationException>(
            () => result.Bind(() => default(UnitResult<string>)));

        var uninitialized = default(UnitResult<string>);
        Assert.Throws<InvalidOperationException>(() => _ = uninitialized.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => uninitialized.TryGetError(out _));
    }

    [Fact]
    public void EqualityHashingFormattingLawsAndSurface_AreStable()
    {
        Func<UnitResult<string>> first = () => UnitResult.Success<string>();
        Func<UnitResult<string>> second = () => UnitResult.Failure("second");
        var success = UnitResult.Success<string>();
        var failure = UnitResult.Failure("error");
        var sameFailure = UnitResult.Failure("error");
        var type = typeof(UnitResult<string>);

        Assert.Equal(first(), success.Bind(first));
        foreach (var result in new[] { success, failure })
        {
            Assert.Equal(result, result.Bind(() => UnitResult.Success<string>()));
            Assert.Equal(
                result.Bind(first).Bind(second),
                result.Bind(() => first().Bind(second)));
        }

        Assert.Equal(failure, sameFailure);
        Assert.Equal(failure.GetHashCode(), sameFailure.GetHashCode());
        Assert.True(failure == sameFailure);
        Assert.True(success != failure);
        Assert.Equal("Success", success.ToString());
        Assert.Equal("Failure(error)", failure.ToString());
        Assert.Equal("Uninitialized", default(UnitResult<string>).ToString());
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Value" or "Error");
        Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == "op_Implicit");
    }
}
