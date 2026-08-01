using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class StatusResultTests
{
    [Fact]
    public void FactoriesAndCaseProperties_CreateSelectedCases()
    {
        var success = Result.Success();
        var failure = Result.Failure();

        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.False(failure.IsSuccess);
        Assert.True(failure.IsFailure);
    }

    [Fact]
    public void Match_EachCase_InvokesSelectedDelegateOnce()
    {
        var successes = 0;
        var failures = 0;

        var successValue = Result.Success().Match(
            () => ++successes,
            () => ++failures);
        Result.Failure().Match(
            () => successes++,
            () => failures++);

        Assert.Equal(1, successValue);
        Assert.Equal(1, successes);
        Assert.Equal(1, failures);
    }

    [Fact]
    public void Bind_EachTargetArity_ShortCircuitsFailureAndMapsItLazily()
    {
        var invocations = 0;
        var errorInvocations = 0;
        Func<Result> statusBind = () =>
        {
            invocations++;
            return Result.Success();
        };
        Func<string> errorFactory = () =>
        {
            errorInvocations++;
            return "error";
        };

        var statusSuccess = Result.Success().Bind(statusBind);
        var statusFailure = Result.Failure().Bind(statusBind);
        var typedSuccess = Result.Success().Bind(
            () => Result.Success<string>(),
            errorFactory);
        var typedFailure = Result.Failure().Bind(
            () => Result.Success<string>(),
            errorFactory);
        var valueSuccess = Result.Success().Bind(
            () => Result.Success<int, string>(42),
            errorFactory);
        var valueFailure = Result.Failure().Bind(
            () => Result.Success<int, string>(42),
            errorFactory);

        Assert.Equal(Result.Success(), statusSuccess);
        Assert.Equal(Result.Failure(), statusFailure);
        Assert.Equal(Result.Success<string>(), typedSuccess);
        Assert.Equal(Result.Failure<string>("error"), typedFailure);
        Assert.Equal(Result.Success<int, string>(42), valueSuccess);
        Assert.Equal(Result.Failure<int, string>("error"), valueFailure);
        Assert.Equal(1, invocations);
        Assert.Equal(2, errorInvocations);
    }

    [Fact]
    public void MapError_EachCase_AttachesErrorOnlyToFailure()
    {
        var invocations = 0;
        Func<string> errorFactory = () =>
        {
            invocations++;
            return "error";
        };

        var success = Result.Success().MapError(errorFactory);
        var failure = Result.Failure().MapError(errorFactory);

        Assert.Equal(Result.Success<string>(), success);
        Assert.Equal(Result.Failure<string>("error"), failure);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void TapRecoverAndRecoverWith_InvokeOnlySelectedDelegates()
    {
        var successes = 0;
        var failures = 0;
        var recoveries = 0;
        var success = Result.Success()
            .Tap(() => successes++)
            .TapFailure(() => failures++)
            .Recover(() => recoveries++)
            .RecoverWith(() =>
            {
                recoveries++;
                return Result.Success();
            });
        var recovered = Result.Failure()
            .Tap(() => successes++)
            .TapFailure(() => failures++)
            .RecoverWith(() =>
            {
                recoveries++;
                return Result.Success();
            });

        Assert.Equal(Result.Success(), success);
        Assert.Equal(Result.Success(), recovered);
        Assert.Equal(1, successes);
        Assert.Equal(1, failures);
        Assert.Equal(1, recoveries);
    }

    [Fact]
    public void Delegates_NullOrThrowing_RejectOrPropagate()
    {
        var expected = new TestException();
        var result = Result.Success();

        Assert.Throws<ArgumentNullException>(() => result.Match<int>(null!, () => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => 0, null!));
        Assert.Throws<ArgumentNullException>(() => result.Match(null!, () => { }));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => { }, null!));
        Assert.Throws<ArgumentNullException>(() => result.Bind(null!));
        Assert.Throws<ArgumentNullException>(() => result.MapError<string>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => result.TapFailure(null!));
        Assert.Throws<ArgumentNullException>(() => result.Recover(null!));
        Assert.Throws<ArgumentNullException>(() => result.RecoverWith(null!));
        Assert.Same(expected, Assert.Throws<TestException>(() => result.Bind(() => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => result.Tap(() => throw expected)));
    }

    [Fact]
    public void Bind_UninitializedDelegateResult_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => Result.Success().Bind(() => default(Result)));
        Assert.Throws<InvalidOperationException>(
            () => Result.Failure().RecoverWith(() => default));
    }

    [Fact]
    public void Default_OperationalMembers_ThrowInvalidOperationException()
    {
        var result = default(Result);
        var array = new Result[1];
        var operations = new Action[]
        {
            () => _ = result.IsSuccess,
            () => _ = result.IsFailure,
            () => result.Match(() => 1, () => 0),
            () => result.Match(() => { }, () => { }),
            () => result.Bind(Result.Success),
            () => result.MapError(() => "error"),
            () => result.Tap(() => { }),
            () => result.TapFailure(() => { }),
            () => result.Recover(() => { }),
            () => result.RecoverWith(Result.Success)
        };

        foreach (var operation in operations)
            Assert.Throws<InvalidOperationException>(operation);

        Assert.Throws<InvalidOperationException>(() => _ = array[0].IsSuccess);
        Assert.Throws<InvalidOperationException>(() => _ = CreateDefault<Result>().IsFailure);
    }

    [Fact]
    public void EqualityHashingOperatorsAndFormatting_IncludeEveryCase()
    {
        var success = Result.Success();
        var failure = Result.Failure();
        var uninitialized = default(Result);

        Assert.Equal(Result.Success(), success);
        Assert.Equal(Result.Success().GetHashCode(), success.GetHashCode());
        Assert.NotEqual(success, failure);
        Assert.True(success == Result.Success());
        Assert.True(success != failure);
        Assert.Equal(default, uninitialized);
        Assert.Equal("Success", success.ToString());
        Assert.Equal("Failure", failure.ToString());
        Assert.Equal("Uninitialized", uninitialized.ToString());
    }

    [Fact]
    public void PublicSurface_HasNoConstructorFieldsPayloadPropertiesOrImplicitConversions()
    {
        var type = typeof(Result);

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Value" or "Error");
        Assert.DoesNotContain(
            type.GetMethods(BindingFlags.Public | BindingFlags.Static),
            method => method.Name == "op_Implicit");
    }

    private static T CreateDefault<T>() where T : struct => default;

    private sealed class TestException : Exception;
}
