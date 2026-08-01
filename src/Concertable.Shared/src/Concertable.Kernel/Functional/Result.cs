using System.Diagnostics.CodeAnalysis;

namespace Concertable.Kernel.Functional;

public readonly struct Result : IEquatable<Result>
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;
    internal const string SuccessText = "Success";
    internal const string FailureText = "Failure";
    internal const string UninitializedText = "Uninitialized";

    private readonly byte tag;

    private Result(byte tag)
    {
        this.tag = tag;
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

    public static Result Success() => new(SuccessTag);

    public static Result Failure() => new(FailureTag);

    public static Result<TError> Success<TError>()
        where TError : notnull =>
        Result<TError>.Success();

    public static Result<TError> Failure<TError>(TError error)
        where TError : notnull =>
        Result<TError>.Failure(error);

    public static Result<TValue, TError> Success<TValue, TError>(TValue value)
        where TValue : notnull
        where TError : notnull =>
        Result<TValue, TError>.Success(value);

    public static Result<TValue, TError> Failure<TValue, TError>(TError error)
        where TValue : notnull
        where TError : notnull =>
        Result<TValue, TError>.Failure(error);

    public TResult Match<TResult>(Func<TResult> success, Func<TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag == SuccessTag ? success() : failure();
    }

    public void Match(Action success, Action failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag == SuccessTag)
            success();
        else
            failure();
    }

    public Result Bind(Func<Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Failure();

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TError> Bind<TError>(
        Func<Result<TError>> bind,
        Func<TError> failureFactory)
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(failureFactory);

        if (this.tag == FailureTag)
            return Failure(failureFactory());

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TValue, TError> Bind<TValue, TError>(
        Func<Result<TValue, TError>> bind,
        Func<TError> failureFactory)
        where TValue : notnull
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(failureFactory);

        if (this.tag == FailureTag)
            return Failure<TValue, TError>(failureFactory());

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TError> MapError<TError>(Func<TError> errorFactory)
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(errorFactory);

        return this.tag == SuccessTag
            ? Result.Success<TError>()
            : Result.Failure(errorFactory());
    }

    public Result Tap(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action();

        return this;
    }

    public Result TapFailure(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action();

        return this;
    }

    public Result Recover(Action fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        fallback();
        return Success();
    }

    public Result RecoverWith(Func<Result> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        var result = fallback();
        result.EnsureInitialized();
        return result;
    }

    public bool Equals(Result other) => this.tag == other.tag;

    public override bool Equals(object? obj) => obj is Result other && this.Equals(other);

    public override int GetHashCode() => HashCode.Combine(this.tag);

    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => SuccessText,
            FailureTag => FailureText,
            _ => UninitializedText
        };

    public static bool operator ==(Result left, Result right) => left.Equals(right);

    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");
    }
}

public readonly struct Result<TError> : IEquatable<Result<TError>>
    where TError : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TError? error;

    private Result(byte tag, TError? error)
    {
        this.tag = tag;
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

    public static Result<TError> Success() => new(SuccessTag, default);

    public static Result<TError> Failure(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TError>(FailureTag, error);
    }

    public TResult Match<TResult>(Func<TResult> success, Func<TError, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag == SuccessTag ? success() : failure(this.error!);
    }

    public void Match(Action success, Action<TError> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (this.tag == SuccessTag)
            success();
        else
            failure(this.error!);
    }

    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag == FailureTag;
    }

    public Result Bind(Func<Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure();

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TError> Bind(Func<Result<TError>> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return this;

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TValue, TError> Bind<TValue>(Func<Result<TValue, TError>> bind)
        where TValue : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure<TValue, TError>(this.error!);

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TNextError> MapError<TNextError>(Func<TError, TNextError> map)
        where TNextError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? Result.Success<TNextError>()
            : Result.Failure(map(this.error!));
    }

    public Result<TError> Tap(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action();

        return this;
    }

    public Result<TError> TapError(Action<TError> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action(this.error!);

        return this;
    }

    public Result<TError> Recover(Action<TError> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        fallback(this.error!);
        return Success();
    }

    public Result<TError> RecoverWith(Func<TError, Result<TError>> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        var result = fallback(this.error!);
        result.EnsureInitialized();
        return result;
    }

    public bool Equals(Result<TError> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag != FailureTag
            || EqualityComparer<TError>.Default.Equals(this.error!, other.error!);
    }

    public override bool Equals(object? obj) =>
        obj is Result<TError> other && this.Equals(other);

    public override int GetHashCode() =>
        this.tag == FailureTag
            ? HashCode.Combine(this.tag, EqualityComparer<TError>.Default.GetHashCode(this.error!))
            : HashCode.Combine(this.tag);

    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => Result.SuccessText,
            FailureTag => $"{Result.FailureText}({this.error})",
            _ => Result.UninitializedText
        };

    public static bool operator ==(Result<TError> left, Result<TError> right) => left.Equals(right);

    public static bool operator !=(Result<TError> left, Result<TError> right) => !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");

        if (this.tag == FailureTag && this.error is null)
            throw new InvalidOperationException("The Result failure has no error.");
    }
}

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

    public Result Bind(Func<TValue, Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure();

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    public Result<TError> Bind(Func<TValue, Result<TError>> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
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
            SuccessTag => $"{Result.SuccessText}({this.value})",
            FailureTag => $"{Result.FailureText}({this.error})",
            _ => Result.UninitializedText
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

        if (this.tag == SuccessTag && this.value is null)
            throw new InvalidOperationException("The Result success has no value.");

        if (this.tag == FailureTag && this.error is null)
            throw new InvalidOperationException("The Result failure has no error.");
    }
}
