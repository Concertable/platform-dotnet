using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class ErrorResultTests
{
    [Fact]
    public void FactoriesPropertiesAndTryGet_CreateSelectedCases()
    {
        var success = Result<string>.Success();
        var failure = Result<string>.Failure("error");

        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.False(success.TryGetError(out var missing));
        Assert.Null(missing);
        Assert.False(failure.IsSuccess);
        Assert.True(failure.IsFailure);
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("error", error);
        Assert.Equal(success, Result.Success<string>());
        Assert.Equal(failure, Result.Failure<string>("error"));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<string>(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Fact]
    public void Match_EachCase_InvokesSelectedDelegateOnce()
    {
        var successes = 0;
        var failures = 0;

        var successValue = Result.Success<string>().Match(
            () => ++successes,
            _ => ++failures);
        Result.Failure<string>("error").Match(
            () => successes++,
            _ => failures++);

        Assert.Equal(1, successValue);
        Assert.Equal(1, successes);
        Assert.Equal(1, failures);
    }

    [Fact]
    public void Bind_EachTargetArity_PreservesSelectedCase()
    {
        var statusSuccess = Result.Success<string>().Bind(Result.Success);
        var statusFailure = Result.Failure<string>("error").Bind(Result.Success);
        var typedSuccess = Result.Success<string>().Bind(() => Result.Success<string>());
        var typedFailure = Result.Failure<string>("error").Bind(() => Result.Success<string>());
        var valueSuccess = Result.Success<string>().Bind(() => Result.Success<int, string>(42));
        var valueFailure = Result.Failure<string>("error").Bind(() => Result.Success<int, string>(42));

        Assert.Equal(Result.Success(), statusSuccess);
        Assert.Equal(Result.Failure(), statusFailure);
        Assert.Equal(Result.Success<string>(), typedSuccess);
        Assert.Equal(Result.Failure<string>("error"), typedFailure);
        Assert.Equal(Result.Success<int, string>(42), valueSuccess);
        Assert.Equal(Result.Failure<int, string>("error"), valueFailure);
    }

    [Fact]
    public void MapErrorTapAndRecovery_InvokeOnlySelectedDelegates()
    {
        var successes = 0;
        var failures = 0;
        var recoveries = 0;
        var success = Result.Success<string>()
            .MapError(error => error.Length)
            .Tap(() => successes++)
            .TapError(_ => failures++);
        var failure = Result.Failure<string>("error")
            .MapError(error => error.Length)
            .Tap(() => successes++)
            .TapError(_ => failures++);
        var recovered = Result.Failure<string>("error")
            .Recover(error => recoveries += error.Length);

        Assert.Equal(Result.Success<int>(), success);
        Assert.Equal(Result.Failure<int>(5), failure);
        Assert.Equal(Result.Success<string>(), recovered);
        Assert.Equal(1, successes);
        Assert.Equal(1, failures);
        Assert.Equal(5, recoveries);
    }

    [Fact]
    public void RecoverWith_EachCase_InvokesFallbackOnlyForFailure()
    {
        var invocations = 0;
        Func<string, Result<string>> fallback = error =>
        {
            invocations++;
            return Result.Failure(error.ToUpperInvariant());
        };

        var success = Result.Success<string>().RecoverWith(fallback);
        var failure = Result.Failure<string>("error").RecoverWith(fallback);

        Assert.Equal(Result.Success<string>(), success);
        Assert.Equal(Result.Failure<string>("ERROR"), failure);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Delegates_NullProducedNullOrThrowing_RejectOrPropagate()
    {
        var expected = new TestException();
        var success = Result.Success<string>();
        var failure = Result.Failure<string>("error");

        Assert.Throws<ArgumentNullException>(() => success.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => success.Match(() => 0, null!));
        Assert.Throws<ArgumentNullException>(() => success.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => success.Match(() => { }, null!));
        Assert.Throws<ArgumentNullException>(() => success.Bind((Func<Result<string>>)null!));
        Assert.Throws<ArgumentNullException>(() => success.MapError<int>(null!));
        Assert.Throws<ArgumentNullException>(() => success.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => success.TapError(null!));
        Assert.Throws<ArgumentNullException>(() => success.Recover(null!));
        Assert.Throws<ArgumentNullException>(() => success.RecoverWith(null!));
        Assert.Throws<ArgumentNullException>(() => failure.MapError(_ => (string)null!));
        Assert.Same(expected, Assert.Throws<TestException>(() => failure.MapError<int>(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => failure.TapError(_ => throw expected)));
    }

    [Fact]
    public void Default_OperationalMembers_ThrowInvalidOperationException()
    {
        var result = default(Result<string>);
        var array = new Result<string>[1];
        var operations = new Action[]
        {
            () => _ = result.IsSuccess,
            () => _ = result.IsFailure,
            () => result.Match(() => 1, _ => 0),
            () => result.Match(() => { }, _ => { }),
            () => result.TryGetError(out _),
            () => result.Bind(() => Result.Success<string>()),
            () => result.MapError(error => error.Length),
            () => result.Tap(() => { }),
            () => result.TapError(_ => { }),
            () => result.Recover(_ => { }),
            () => result.RecoverWith(_ => Result.Success<string>())
        };

        foreach (var operation in operations)
            Assert.Throws<InvalidOperationException>(operation);

        Assert.Throws<InvalidOperationException>(() => _ = array[0].IsSuccess);
        Assert.Throws<InvalidOperationException>(() => _ = CreateDefault<string>().IsFailure);
        Assert.Throws<InvalidOperationException>(
            () => Result.Success<string>().Bind(() => default(Result<string>)));
    }

    [Fact]
    public void EqualityHashingOperatorsAndFormatting_IncludeCaseAndPayload()
    {
        var success = Result.Success<string>();
        var failure = Result.Failure<string>("error");
        var sameFailure = Result.Failure<string>("error");
        var uninitialized = default(Result<string>);

        Assert.Equal(Result.Success<string>(), success);
        Assert.Equal(failure, sameFailure);
        Assert.Equal(failure.GetHashCode(), sameFailure.GetHashCode());
        Assert.NotEqual(success, failure);
        Assert.True(failure == sameFailure);
        Assert.True(success != failure);
        Assert.Equal(default, uninitialized);
        Assert.Equal("Success", success.ToString());
        Assert.Equal("Failure(error)", failure.ToString());
        Assert.Equal("Uninitialized", uninitialized.ToString());
    }

    [Fact]
    public void MonadLaws_LeftRightIdentityAndAssociativity_Hold()
    {
        Func<Result<string>> first = () => Result.Success<string>();
        Func<Result<string>> second = () => Result.Failure<string>("second");

        Assert.Equal(first(), Result.Success<string>().Bind(first));

        foreach (var result in new[] { Result.Success<string>(), Result.Failure<string>("error") })
        {
            Assert.Equal(result, result.Bind(Result.Success<string>));
            Assert.Equal(
                result.Bind(first).Bind(second),
                result.Bind(() => first().Bind(second)));
        }
    }

    [Fact]
    public void PublicSurface_HasNoConstructorFieldsPayloadPropertiesOrImplicitConversions()
    {
        var type = typeof(Result<string>);

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Value" or "Error");
        Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == "op_Implicit");
    }

    private static Result<TError> CreateDefault<TError>() where TError : notnull => default;

    private sealed class TestException : Exception;
}
