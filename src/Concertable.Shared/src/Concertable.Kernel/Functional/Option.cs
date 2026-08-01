using System.Diagnostics.CodeAnalysis;

namespace Concertable.Kernel.Functional;

public readonly struct Option<T> : IEquatable<Option<T>>
    where T : notnull
{
    private const byte SomeTag = 1;

    private readonly byte tag;
    private readonly T? value;

    private Option(T value)
    {
        this.tag = SomeTag;
        this.value = value;
    }

    public bool IsSome => this.tag == SomeTag;

    public bool IsNone => !this.IsSome;

    internal static Option<T> CreateSome(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Option<T>(value);
    }

    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return this.IsSome ? some(this.value!) : none();
    }

    public void Match(Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        if (this.IsSome)
            some(this.value!);
        else
            none();
    }

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = this.value;
        return this.IsSome;
    }

    public Option<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return this.IsSome
            ? Option.Some(map(this.value!))
            : Option.None<TResult>();
    }

    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bind)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return this.IsSome
            ? bind(this.value!)
            : Option.None<TResult>();
    }

    public Option<T> OrElse(Func<Option<T>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return this.IsSome ? this : fallback();
    }

    public Result<T, TError> OrFailure<TError>(TError error)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(error);

        return this.IsSome
            ? Result.Success<T, TError>(this.value!)
            : Result.Failure<T, TError>(error);
    }

    public Result<T, TError> OrFailure<TError>(Func<TError> errorFactory)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return this.IsSome
            ? Result.Success<T, TError>(this.value!)
            : Result.Failure<T, TError>(errorFactory());
    }

    public T ValueOr(T fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return this.IsSome ? this.value! : fallback;
    }

    public T ValueOrElse(Func<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.IsSome)
            return this.value!;

        var value = fallback();
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    public bool Equals(Option<T> other) =>
        this.tag == other.tag
        && (this.IsNone || EqualityComparer<T>.Default.Equals(this.value!, other.value!));

    public override bool Equals(object? obj) => obj is Option<T> other && this.Equals(other);

    public override int GetHashCode() =>
        this.IsSome
            ? HashCode.Combine(this.tag, EqualityComparer<T>.Default.GetHashCode(this.value!))
            : HashCode.Combine(this.tag);

    public override string ToString() => this.IsSome ? $"Some({this.value})" : "None";

    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
}

public static class Option
{
    public static Option<T> Some<T>(T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        return Option<T>.CreateSome(value);
    }

    public static Option<T> None<T>()
        where T : notnull =>
        default;

    public static Option<T> FromNullable<T>(T? value)
        where T : class =>
        value is null ? None<T>() : Some(value);

    public static Option<T> FromNullable<T>(T? value)
        where T : struct =>
        value.HasValue ? Some(value.Value) : None<T>();
}

public static class OptionExtensions
{
    public static Option<T> ToOption<T>(this T? value)
        where T : class =>
        Option.FromNullable(value);

    public static Option<T> ToOption<T>(this T? value)
        where T : struct =>
        Option.FromNullable(value);

    public static async Task<Option<T>> ToOption<T>(this Task<T?> source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ToOption();
    }
}
