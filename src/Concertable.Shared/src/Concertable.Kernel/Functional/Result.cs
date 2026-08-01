using System.Diagnostics.CodeAnalysis;

namespace Concertable.Kernel.Functional;

public readonly struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
    where TValue : notnull
    where TError : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TValue? value;
    private readonly TError? error;

    private Result(byte tag, TValue? value, TError? error)
    {
        this.tag = tag;
        this.value = value;
        this.error = error;
    }

    public bool IsSuccess
    {
        get
        {
            this.EnsureInitialized();
            return this.tag == SuccessTag;
        }
    }

    public bool IsFailure
    {
        get
        {
            this.EnsureInitialized();
            return this.tag == FailureTag;
        }
    }

    public static Result<TValue, TError> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TValue, TError>(SuccessTag, value, default);
    }

    public static Result<TValue, TError> Failure(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue, TError>(FailureTag, default, error);
    }

    public TResult Match<TResult>(
        Func<TValue, TResult> success,
        Func<TError, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag == SuccessTag ? success(this.value!) : failure(this.error!);
    }

    public void Match(Action<TValue> success, Action<TError> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag == SuccessTag)
            success(this.value!);
        else
            failure(this.error!);
    }

    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        this.EnsureInitialized();
        value = this.value;
        return this.tag == SuccessTag;
    }

    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag == FailureTag;
    }

    public Result<TNext, TError> Map<TNext>(Func<TValue, TNext> map)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? Result.Success<TNext, TError>(map(this.value!))
            : Result.Failure<TNext, TError>(this.error!);
    }

    public Result<TNext, TError> Bind<TNext>(Func<TValue, Result<TNext, TError>> bind)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure<TNext, TError>(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    public Result<TValue, TNextError> MapError<TNextError>(Func<TError, TNextError> map)
        where TNextError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? Result.Success<TValue, TNextError>(this.value!)
            : Result.Failure<TValue, TNextError>(map(this.error!));
    }

    public Result<TValue, TError> Ensure(
        Func<TValue, bool> predicate,
        Func<TError> errorFactory)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (this.tag == FailureTag || predicate(this.value!))
            return this;

        return Failure(errorFactory());
    }

    public Result<TValue, TError> Tap(Action<TValue> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action(this.value!);

        return this;
    }

    public Result<TValue, TError> TapError(Action<TError> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action(this.error!);

        return this;
    }

    public Result<TValue, TError> Recover(Func<TError, TValue> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return this.tag == SuccessTag ? this : Success(fallback(this.error!));
    }

    public Result<TValue, TError> RecoverWith(
        Func<TError, Result<TValue, TError>> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        var result = fallback(this.error!);
        result.EnsureInitialized();
        return result;
    }

    public bool Equals(Result<TValue, TError> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag switch
        {
            SuccessTag => EqualityComparer<TValue>.Default.Equals(this.value!, other.value!),
            FailureTag => EqualityComparer<TError>.Default.Equals(this.error!, other.error!),
            _ => true
        };
    }

    public override bool Equals(object? obj) =>
        obj is Result<TValue, TError> other && this.Equals(other);

    public override int GetHashCode() =>
        this.tag switch
        {
            SuccessTag => HashCode.Combine(
                this.tag,
                EqualityComparer<TValue>.Default.GetHashCode(this.value!)),
            FailureTag => HashCode.Combine(
                this.tag,
                EqualityComparer<TError>.Default.GetHashCode(this.error!)),
            _ => HashCode.Combine(this.tag)
        };

    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => $"Success({this.value})",
            FailureTag => $"Failure({this.error})",
            _ => "Uninitialized"
        };

    public static bool operator ==(
        Result<TValue, TError> left,
        Result<TValue, TError> right) =>
        left.Equals(right);

    public static bool operator !=(
        Result<TValue, TError> left,
        Result<TValue, TError> right) =>
        !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");
    }
}

public static class Result
{
    public static Result<TValue, TError> Success<TValue, TError>(TValue value)
        where TValue : notnull
        where TError : notnull =>
        Result<TValue, TError>.Success(value);

    public static Result<TValue, TError> Failure<TValue, TError>(TError error)
        where TValue : notnull
        where TError : notnull =>
        Result<TValue, TError>.Failure(error);

    public static Result<Unit, TError> Success<TError>()
        where TError : notnull =>
        Result<Unit, TError>.Success(Unit.Value);
}
