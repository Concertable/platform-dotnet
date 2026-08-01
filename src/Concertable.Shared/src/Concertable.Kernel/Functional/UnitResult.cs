using System.Diagnostics.CodeAnalysis;

namespace Concertable.Kernel.Functional;

public static class UnitResult
{
    public static UnitResult<TError> Success<TError>()
        where TError : notnull =>
        UnitResult<TError>.Success();

    public static UnitResult<TError> Failure<TError>(TError error)
        where TError : notnull =>
        UnitResult<TError>.Failure(error);
}

public readonly struct UnitResult<TError> : IEquatable<UnitResult<TError>>
    where TError : notnull
{
    private const byte SuccessTag = 1;
    private const byte FailureTag = 2;

    private readonly byte tag;
    private readonly TError? error;

    private UnitResult(byte tag, TError? error)
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

    public static UnitResult<TError> Success() => new(SuccessTag, default);

    public static UnitResult<TError> Failure(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new UnitResult<TError>(FailureTag, error);
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

    public Result Bind(Func<Result> bind, Func<TError, string> mapError)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag == FailureTag)
            return Result.Failure(mapError(this.error!));

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public Result<TValue> Bind<TValue>(
        Func<Result<TValue>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        if (this.tag == FailureTag)
            return Result.Failure<TValue>(mapError(this.error!));

        var result = bind();
        result.EnsureInitialized();
        return result;
    }

    public UnitResult<TError> Bind(Func<UnitResult<TError>> bind)
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

    public UnitResult<TNextError> MapError<TNextError>(Func<TError, TNextError> map)
        where TNextError : notnull
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return this.tag == SuccessTag
            ? UnitResult.Success<TNextError>()
            : UnitResult.Failure(map(this.error!));
    }

    public UnitResult<TError> Tap(Action action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == SuccessTag)
            action();

        return this;
    }

    public UnitResult<TError> TapError(Action<TError> action)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        if (this.tag == FailureTag)
            action(this.error!);

        return this;
    }

    public UnitResult<TError> Recover(Action<TError> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        fallback(this.error!);
        return Success();
    }

    public UnitResult<TError> RecoverWith(Func<TError, UnitResult<TError>> fallback)
    {
        this.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.tag == SuccessTag)
            return this;

        var result = fallback(this.error!);
        result.EnsureInitialized();
        return result;
    }

    public bool Equals(UnitResult<TError> other)
    {
        if (this.tag != other.tag)
            return false;

        return this.tag != FailureTag
            || EqualityComparer<TError>.Default.Equals(this.error!, other.error!);
    }

    public override bool Equals(object? obj) =>
        obj is UnitResult<TError> other && this.Equals(other);

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

    public static bool operator ==(UnitResult<TError> left, UnitResult<TError> right) =>
        left.Equals(right);

    public static bool operator !=(UnitResult<TError> left, UnitResult<TError> right) =>
        !left.Equals(right);

    internal void EnsureInitialized()
    {
        if (this.tag is not SuccessTag and not FailureTag)
            throw new InvalidOperationException("The UnitResult is uninitialized.");

        if (this.tag == FailureTag && this.error is null)
            throw new InvalidOperationException("The UnitResult failure has no error.");
    }
}
