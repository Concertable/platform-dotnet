namespace Concertable.Kernel.Functional;

public static class OptionTaskExtensions
{
    public static async Task<TResult> Match<T, TResult>(
        this Task<Option<T>> source,
        Func<T, TResult> some,
        Func<TResult> none)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(some, none);
    }

    public static async Task Match<T>(
        this Task<Option<T>> source,
        Action<T> some,
        Action none)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(some, none);
    }

    public static async Task<Option<TResult>> Map<T, TResult>(
        this Task<Option<T>> source,
        Func<T, TResult> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Map(map);
    }

    public static async Task<Option<TResult>> Bind<T, TResult>(
        this Task<Option<T>> source,
        Func<T, Option<TResult>> bind)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<Option<T>> OrElse<T>(
        this Task<Option<T>> source,
        Func<Option<T>> fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).OrElse(fallback);
    }

    public static async Task<Result<T, TError>> OrFailure<T, TError>(
        this Task<Option<T>> source,
        TError error)
        where T : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).OrFailure(error);
    }

    public static async Task<Result<T, TError>> OrFailure<T, TError>(
        this Task<Option<T>> source,
        Func<TError> errorFactory)
        where T : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).OrFailure(errorFactory);
    }

    public static async Task<T> ValueOr<T>(this Task<Option<T>> source, T fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ValueOr(fallback);
    }

    public static async Task<T> ValueOrElse<T>(
        this Task<Option<T>> source,
        Func<T> fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ValueOrElse(fallback);
    }

    public static Task<TResult> MatchAsync<T, TResult>(
        this Option<T> option,
        Func<T, Task<TResult>> some,
        Func<Task<TResult>> none)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return option.Match(
            value => RequireTask(some(value)),
            () => RequireTask(none()));
    }

    public static Task MatchAsync<T>(
        this Option<T> option,
        Func<T, Task> some,
        Func<Task> none)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return option.Match(
            value => RequireTask(some(value)),
            () => RequireTask(none()));
    }

    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Option<T>> source,
        Func<T, Task<TResult>> some,
        Func<Task<TResult>> none)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(some, none)
            .ConfigureAwait(false);
    }

    public static async Task MatchAsync<T>(
        this Task<Option<T>> source,
        Func<T, Task> some,
        Func<Task> none)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(some, none)
            .ConfigureAwait(false);
    }

    public static Task<Option<TResult>> MapAsync<T, TResult>(
        this Option<T> option,
        Func<T, Task<TResult>> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return option.Match(
            value => MapSomeAsync<T, TResult>(value, map),
            () => Task.FromResult(Option.None<TResult>()));
    }

    public static async Task<Option<TResult>> MapAsync<T, TResult>(
        this Task<Option<T>> source,
        Func<T, Task<TResult>> map)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MapAsync(map)
            .ConfigureAwait(false);
    }

    public static Task<Option<TResult>> BindAsync<T, TResult>(
        this Option<T> option,
        Func<T, Task<Option<TResult>>> bind)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return option.Match(
            value => RequireTask(bind(value)),
            () => Task.FromResult(Option.None<TResult>()));
    }

    public static async Task<Option<TResult>> BindAsync<T, TResult>(
        this Task<Option<T>> source,
        Func<T, Task<Option<TResult>>> bind)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<Option<T>> OrElseAsync<T>(
        this Option<T> option,
        Func<Task<Option<T>>> fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return option.Match(
            value => Task.FromResult(Option.Some(value)),
            () => RequireTask(fallback()));
    }

    public static async Task<Option<T>> OrElseAsync<T>(
        this Task<Option<T>> source,
        Func<Task<Option<T>>> fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .OrElseAsync(fallback)
            .ConfigureAwait(false);
    }

    public static Task<Result<T, TError>> OrFailureAsync<T, TError>(
        this Option<T> option,
        Func<Task<TError>> errorFactory)
        where T : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return option.Match(
            value => Task.FromResult(Result.Success<T, TError>(value)),
            () => CreateFailureAsync<T, TError>(errorFactory));
    }

    public static async Task<Result<T, TError>> OrFailureAsync<T, TError>(
        this Task<Option<T>> source,
        Func<Task<TError>> errorFactory)
        where T : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .OrFailureAsync(errorFactory)
            .ConfigureAwait(false);
    }

    public static Task<T> ValueOrElseAsync<T>(
        this Option<T> option,
        Func<Task<T>> fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return option.Match(
            Task.FromResult,
            () => RequireTask(fallback()));
    }

    public static async Task<T> ValueOrElseAsync<T>(
        this Task<Option<T>> source,
        Func<Task<T>> fallback)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .ValueOrElseAsync(fallback)
            .ConfigureAwait(false);
    }

    private static Task<T> RequireTask<T>(Task<T>? task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return task;
    }

    private static Task RequireTask(Task? task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return task;
    }

    private static async Task<Option<TResult>> MapSomeAsync<T, TResult>(
        T value,
        Func<T, Task<TResult>> map)
        where T : notnull
        where TResult : notnull =>
        Option.Some(await RequireTask(map(value)).ConfigureAwait(false));

    private static async Task<Result<T, TError>> CreateFailureAsync<T, TError>(
        Func<Task<TError>> errorFactory)
        where T : notnull
        where TError : notnull =>
        Result.Failure<T, TError>(
            await RequireTask(errorFactory()).ConfigureAwait(false));
}
