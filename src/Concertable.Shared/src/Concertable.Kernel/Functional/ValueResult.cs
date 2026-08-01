using System.Diagnostics.CodeAnalysis;

namespace Concertable.Kernel.Functional;

public readonly struct Result<TValue> : IEquatable<Result<TValue>>
    where TValue : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TValue? value;
    private readonly string? error;

    private Result(byte tag, TValue? value, string? error)
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

    public static Result<TValue> Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<TValue>(SuccessTag, value, default);
    }

    public static Result<TValue> Failure(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result<TValue>(FailureTag, default, error);
    }

    public TResult Match<TResult>(
        Func<TValue, TResult> success,
        Func<string, TResult> failure)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return this.tag == SuccessTag ? success(this.value!) : failure(this.error!);
    }

    public void Match(Action<TValue> success, Action<string> failure)
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

    public bool TryGetError([NotNullWhen(true)] out string? error)
    {
        this.EnsureInitialized();
        error = this.error;
        return this.tag == FailureTag;
    }

    public Result<TNext> Map<TNext>(Func<TValue, TNext> map)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? Result.Success(map(this.value!))
            : Result.Failure<TNext>(this.error!);
    }

    public Result<TNext> Bind<TNext>(Func<TValue, Result<TNext>> bind)
        where TNext : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure<TNext>(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    public Result Bind(Func<TValue, Result> bind)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        if (this.tag == FailureTag)
            return Result.Failure(this.error!);

        var result = bind(this.value!);
        result.EnsureInitialized();
        return result;
    }

    public Result<TValue, TError> MapError<TError>(Func<string, TError> map)
        where TError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? Result.Success<TValue, TError>(this.value!)
            : Result.Failure<TValue, TError>(map(this.error!));
    }

    public Result<TValue> Ensure(Func<TValue, bool> predicate, Func<string> errorFactory)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (this.tag == FailureTag || predicate(this.value!))
            return this;

        return Failure(errorFactory());
    }

    public Result<TValue> Tap(Action<TValue> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action(this.value!);

        return this;
    }

    public Result<TValue> TapError(Action<string> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action(this.error!);

        return this;
    }

    public Result<TValue> Recover(Func<string, TValue> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return this.tag == SuccessTag ? this : Success(fallback(this.error!));
    }

    public Result<TValue> RecoverWith(Func<string, Result<TValue>> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        var result = fallback(this.error!);
        result.EnsureInitialized();
        return result;
    }

    public bool Equals(Result<TValue> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag switch
        {
            SuccessTag => EqualityComparer<TValue>.Default.Equals(this.value!, other.value!),
            FailureTag => this.error == other.error,
            _ => true
        };
    }

    public override bool Equals(object? obj) =>
        obj is Result<TValue> other && this.Equals(other);

    public override int GetHashCode() =>
        this.tag switch
        {
            SuccessTag => HashCode.Combine(
                this.tag,
                EqualityComparer<TValue>.Default.GetHashCode(this.value!)),
            FailureTag => HashCode.Combine(this.tag, this.error),
            _ => HashCode.Combine(this.tag)
        };

    public override string ToString() =>
        this.tag switch
        {
            SuccessTag => $"{Result.SuccessText}({this.value})",
            FailureTag => $"{Result.FailureText}({this.error})",
            _ => Result.UninitializedText
        };

    public static bool operator ==(Result<TValue> left, Result<TValue> right) => left.Equals(right);

    public static bool operator !=(Result<TValue> left, Result<TValue> right) => !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The Result is uninitialized.");

        if (this.tag == SuccessTag && this.value is null)
            throw new InvalidOperationException("The Result success has no value.");

        if (this.tag == FailureTag && string.IsNullOrWhiteSpace(this.error))
            throw new InvalidOperationException("The Result failure has no error.");
    }
}
