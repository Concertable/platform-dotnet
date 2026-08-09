using Concertable.Kernel.Functional;
using System.Reflection;

namespace Concertable.Kernel.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Factories_ValueAndError_CreateSelectedCases()
    {
        var genericSuccess = Result<int, string>.Success(42);
        var genericFailure = Result<int, string>.Failure("error");
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>("error");
        var noValue = UnitResult.Success<string>();

        Assert.True(genericSuccess.IsSuccess);
        Assert.True(genericFailure.IsFailure);
        Assert.Equal(genericSuccess, success);
        Assert.Equal(genericFailure, failure);
        Assert.Equal(UnitResult<string>.Success(), noValue);
    }

    [Fact]
    public void Factories_NullPayloads_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success<string, string>(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<string, string>(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string, string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string, string>.Failure(null!));
    }

    [Fact]
    public void CaseProperties_EachCase_ReportOnlySelectedCase()
    {
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>("error");

        Assert.True(success.IsSuccess);
        Assert.False(success.IsFailure);
        Assert.False(failure.IsSuccess);
        Assert.True(failure.IsFailure);
    }

    [Fact]
    public void TryGet_EachCase_ExposesOnlySelectedPayload()
    {
        var success = Result.Success<int, string>(42);
        var failure = Result.Failure<int, string>("error");

        Assert.True(success.TryGetValue(out var value));
        Assert.Equal(42, value);
        Assert.False(success.TryGetError(out var missingError));
        Assert.Null(missingError);
        Assert.False(failure.TryGetValue(out _));
        Assert.True(failure.TryGetError(out var error));
        Assert.Equal("error", error);
    }

    [Fact]
    public void Match_EachCase_InvokesSelectedFunctionOnce()
    {
        var successInvocations = 0;
        var failureInvocations = 0;
        Func<int, string> success = value =>
        {
            successInvocations++;
            return value.ToString();
        };
        Func<string, string> failure = error =>
        {
            failureInvocations++;
            return error;
        };

        var successValue = Result.Success<int, string>(42).Match(success, failure);
        var failureValue = Result.Failure<int, string>("error").Match(success, failure);

        Assert.Equal("42", successValue);
        Assert.Equal("error", failureValue);
        Assert.Equal(1, successInvocations);
        Assert.Equal(1, failureInvocations);
    }

    [Fact]
    public void Match_ActionOverload_InvokesSelectedActionOnce()
    {
        var successTotal = 0;
        var failureValue = string.Empty;

        Result.Success<int, string>(2).Match(value => successTotal += value, error => failureValue = error);
        Result.Failure<int, string>("error").Match(value => successTotal += value, error => failureValue = error);

        Assert.Equal(2, successTotal);
        Assert.Equal("error", failureValue);
    }

    [Fact]
    public void Map_EachCase_MapsOnlySuccess()
    {
        var invocations = 0;
        Func<int, string> map = value =>
        {
            invocations++;
            return value.ToString();
        };

        var success = Result.Success<int, string>(2).Map(map);
        var failure = Result.Failure<int, string>("error").Map(map);

        Assert.Equal(Result.Success<string, string>("2"), success);
        Assert.Equal(Result.Failure<string, string>("error"), failure);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Bind_EachCase_BindsOnlySuccess()
    {
        var invocations = 0;
        Func<int, Result<string, string>> bind = value =>
        {
            invocations++;
            return Result.Success<string, string>(value.ToString());
        };

        var success = Result.Success<int, string>(2).Bind(bind);
        var failure = Result.Failure<int, string>("error").Bind(bind);

        Assert.Equal(Result.Success<string, string>("2"), success);
        Assert.Equal(Result.Failure<string, string>("error"), failure);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Bind_NoValueResult_PreservesSelectedCase()
    {
        var success = Result.Success<int, string>(2)
            .Bind(_ => UnitResult.Success<string>());
        var failure = Result.Failure<int, string>("error")
            .Bind(_ => UnitResult.Success<string>());

        Assert.Equal(UnitResult.Success<string>(), success);
        Assert.Equal(UnitResult.Failure("error"), failure);
    }

    [Fact]
    public void MapError_EachCase_MapsOnlyFailure()
    {
        var invocations = 0;
        Func<string, int> map = error =>
        {
            invocations++;
            return error.Length;
        };

        var success = Result.Success<int, string>(2).MapError(map);
        var failure = Result.Failure<int, string>("error").MapError(map);

        Assert.Equal(Result.Success<int, int>(2), success);
        Assert.Equal(Result.Failure<int, int>(5), failure);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Ensure_SuccessCases_InvokePredicateAndLazyErrorOnlyWhenNeeded()
    {
        var predicateInvocations = 0;
        var errorInvocations = 0;
        Func<int, bool> predicate = value =>
        {
            predicateInvocations++;
            return value > 0;
        };
        Func<string> errorFactory = () =>
        {
            errorInvocations++;
            return "invalid";
        };

        var valid = Result.Success<int, string>(1).Ensure(predicate, errorFactory);
        var invalid = Result.Success<int, string>(0).Ensure(predicate, errorFactory);
        var failure = Result.Failure<int, string>("existing").Ensure(predicate, errorFactory);

        Assert.Equal(Result.Success<int, string>(1), valid);
        Assert.Equal(Result.Failure<int, string>("invalid"), invalid);
        Assert.Equal(Result.Failure<int, string>("existing"), failure);
        Assert.Equal(2, predicateInvocations);
        Assert.Equal(1, errorInvocations);
    }

    [Fact]
    public void TapAndTapError_EachCase_InvokeOnlySelectedSideEffect()
    {
        var successTotal = 0;
        var errors = new List<string>();
        Action<int> successAction = value => successTotal += value;
        Action<string> errorAction = errors.Add;
        var success = Result.Success<int, string>(2);
        var failure = Result.Failure<int, string>("error");

        var tappedSuccess = success.Tap(successAction).TapError(errorAction);
        var tappedFailure = failure.Tap(successAction).TapError(errorAction);

        Assert.Equal(success, tappedSuccess);
        Assert.Equal(failure, tappedFailure);
        Assert.Equal(2, successTotal);
        Assert.Equal(["error"], errors);
    }

    [Fact]
    public void Recover_EachCase_InvokesFallbackOnlyForFailure()
    {
        var invocations = 0;
        Func<string, int> fallback = error =>
        {
            invocations++;
            return error.Length;
        };

        var success = Result.Success<int, string>(2).Recover(fallback);
        var recovered = Result.Failure<int, string>("error").Recover(fallback);

        Assert.Equal(Result.Success<int, string>(2), success);
        Assert.Equal(Result.Success<int, string>(5), recovered);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void RecoverWith_EachCase_InvokesFallbackOnlyForFailure()
    {
        var invocations = 0;
        Func<string, Result<int, string>> fallback = error =>
        {
            invocations++;
            return Result.Success<int, string>(error.Length);
        };

        var success = Result.Success<int, string>(2).RecoverWith(fallback);
        var recovered = Result.Failure<int, string>("error").RecoverWith(fallback);

        Assert.Equal(Result.Success<int, string>(2), success);
        Assert.Equal(Result.Success<int, string>(5), recovered);
        Assert.Equal(1, invocations);
    }

    [Fact]
    public void Combinators_NullDelegates_ThrowArgumentNullException()
    {
        var result = Result.Success<int, string>(1);

        Assert.Throws<ArgumentNullException>(() => result.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(value => value, null!));
        Assert.Throws<ArgumentNullException>(() => result.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => result.Match(_ => { }, null!));
        Assert.Throws<ArgumentNullException>(() => result.Map<string>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Bind<string>(null!));
        Assert.Throws<ArgumentNullException>(() => result.MapError<int>(null!));
        Assert.Throws<ArgumentNullException>(() => result.Ensure(null!, () => "error"));
        Assert.Throws<ArgumentNullException>(() => result.Ensure(_ => true, null!));
        Assert.Throws<ArgumentNullException>(() => result.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => result.TapError(null!));
        Assert.Throws<ArgumentNullException>(() => result.Recover(null!));
        Assert.Throws<ArgumentNullException>(() => result.RecoverWith(null!));
    }

    [Fact]
    public void Combinators_NullProducedPayloads_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => Result.Success<string, string>("value").Map(_ => (string)null!));
        Assert.Throws<ArgumentNullException>(
            () => Result.Failure<string, string>("error").MapError(_ => (string)null!));
        Assert.Throws<ArgumentNullException>(
            () => Result.Success<int, string>(0).Ensure(_ => false, () => null!));
        Assert.Throws<ArgumentNullException>(
            () => Result.Failure<string, string>("error").Recover(_ => null!));
    }

    [Fact]
    public void SelectedDelegates_ThrownException_PropagatesUnchanged()
    {
        var expected = new TestException();

        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Success<int, string>(1).Map<string>(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Success<int, string>(1).Bind<string>(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Failure<int, string>("error").MapError<int>(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Success<int, string>(1).Ensure(_ => throw expected, () => "error")));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Success<int, string>(1).Tap(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Failure<int, string>("error").TapError(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Failure<int, string>("error").Recover(_ => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(() => Result.Failure<int, string>("error").RecoverWith(_ => throw expected)));
    }

    [Fact]
    public void Default_OperationalMembers_ThrowInvalidOperationException()
    {
        var result = default(Result<int, string>);
        var operations = new Action[]
        {
            () => _ = result.IsSuccess,
            () => _ = result.IsFailure,
            () => result.Match(value => value, _ => 0),
            () => result.Match(_ => { }, _ => { }),
            () => result.TryGetValue(out _),
            () => result.TryGetError(out _),
            () => result.Map(value => value),
            () => result.Bind(value => Result.Success<int, string>(value)),
            () => result.MapError(error => error),
            () => result.Ensure(_ => true, () => "error"),
            () => result.Tap(_ => { }),
            () => result.TapError(_ => { }),
            () => result.Recover(_ => 0),
            () => result.RecoverWith(_ => Result.Success<int, string>(0))
        };

        foreach (var operation in operations)
            Assert.Throws<InvalidOperationException>(operation);
    }

    [Fact]
    public void Default_ArraysFieldsAndGenericDefaults_RemainUninitialized()
    {
        var array = new Result<int, string>[1];
        var holder = new ResultHolder();

        Assert.Throws<InvalidOperationException>(() => _ = array[0].IsSuccess);
        Assert.Throws<InvalidOperationException>(() => _ = holder.Value.IsFailure);
        Assert.Throws<InvalidOperationException>(() => _ = CreateDefault<int, string>().IsSuccess);
    }

    [Fact]
    public void BindAndRecoverWith_UninitializedDelegateResult_ThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => Result.Success<int, string>(1).Bind(_ => default(Result<string, string>)));
        Assert.Throws<InvalidOperationException>(
            () => Result.Failure<int, string>("error").RecoverWith(_ => default));
    }

    [Fact]
    public void EqualityHashingOperatorsAndFormatting_IncludeCaseAndPayload()
    {
        var success = Result.Success<string, string>("same");
        var sameSuccess = Result.Success<string, string>("same");
        var failure = Result.Failure<string, string>("same");
        var sameFailure = Result.Failure<string, string>("same");
        var uninitialized = default(Result<string, string>);

        Assert.Equal(success, sameSuccess);
        Assert.Equal(success.GetHashCode(), sameSuccess.GetHashCode());
        Assert.Equal(failure, sameFailure);
        Assert.Equal(failure.GetHashCode(), sameFailure.GetHashCode());
        Assert.NotEqual(success, failure);
        Assert.True(success == sameSuccess);
        Assert.True(success != failure);
        Assert.Equal(default(Result<string, string>), uninitialized);
        Assert.Equal(default(Result<string, string>).GetHashCode(), uninitialized.GetHashCode());
        Assert.Equal("Success(same)", success.ToString());
        Assert.Equal("Failure(same)", failure.ToString());
        Assert.Equal("Uninitialized", uninitialized.ToString());
    }

    [Fact]
    public void PublicSurface_HasNoConstructorFieldsPayloadPropertiesOrImplicitConversions()
    {
        var type = typeof(Result<int, string>);

        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name is "Value" or "Error");
        Assert.DoesNotContain(type.GetMethods(BindingFlags.Public | BindingFlags.Static), method => method.Name == "op_Implicit");
    }

    [Fact]
    public void FunctorLaws_IdentityAndComposition_Hold()
    {
        Func<int, int> first = value => value + 1;
        Func<int, string> second = value => $"{value}!";

        foreach (var result in new[] { Result.Success<int, string>(2), Result.Failure<int, string>("error") })
        {
            Assert.Equal(result, result.Map(value => value));
            Assert.Equal(result.Map(first).Map(second), result.Map(value => second(first(value))));
        }
    }

    [Fact]
    public void MonadLaws_LeftRightIdentityAndAssociativity_Hold()
    {
        Func<int, Result<int, string>> first = value => Result.Success<int, string>(value + 1);
        Func<int, Result<string, string>> second = value => Result.Success<string, string>($"{value}!");
        var value = 2;

        Assert.Equal(first(value), Result.Success<int, string>(value).Bind(first));

        foreach (var result in new[] { Result.Success<int, string>(value), Result.Failure<int, string>("error") })
        {
            Assert.Equal(result, result.Bind(Result.Success<int, string>));
            Assert.Equal(
                result.Bind(first).Bind(second),
                result.Bind(item => first(item).Bind(second)));
        }
    }

    private static Result<TValue, TError> CreateDefault<TValue, TError>()
        where TValue : notnull
        where TError : notnull =>
        default;

    private sealed class ResultHolder
    {
        public Result<int, string> Value { get; }
    }

    private sealed class TestException : Exception;
}
