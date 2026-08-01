namespace Concertable.Kernel.Functional;

public static partial class ResultTaskExtensions
{
    public static async Task<TResult> Match<TResult>(
        this Task<Result> source,
        Func<TResult> success,
        Func<TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task Match(
        this Task<Result> source,
        Action success,
        Action failure)
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task<Result> Bind(
        this Task<Result> source,
        Func<Result> bind)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<Result<TError>> Bind<TError>(
        this Task<Result> source,
        Func<Result<TError>> bind,
        Func<TError> failureFactory)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, failureFactory);
    }

    public static async Task<Result<TValue, TError>> Bind<TValue, TError>(
        this Task<Result> source,
        Func<Result<TValue, TError>> bind,
        Func<TError> failureFactory)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, failureFactory);
    }

    public static async Task<Result<TError>> MapError<TError>(
        this Task<Result> source,
        Func<TError> errorFactory)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(errorFactory);
    }

    public static async Task<Result> Tap(this Task<Result> source, Action action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    public static async Task<Result> TapFailure(this Task<Result> source, Action action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapFailure(action);
    }

    public static async Task<Result> Recover(this Task<Result> source, Action fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    public static async Task<Result> RecoverWith(
        this Task<Result> source,
        Func<Result> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    public static Task<TResult> MatchAsync<TResult>(
        this Result result,
        Func<Task<TResult>> success,
        Func<Task<TResult>> failure)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            () => RequireTask(success()),
            () => RequireTask(failure()));
    }

    public static Task MatchAsync(
        this Result result,
        Func<Task> success,
        Func<Task> failure)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            () => RequireTask(success()),
            () => RequireTask(failure()));
    }

    public static async Task<TResult> MatchAsync<TResult>(
        this Task<Result> source,
        Func<Task<TResult>> success,
        Func<Task<TResult>> failure)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static async Task MatchAsync(
        this Task<Result> source,
        Func<Task> success,
        Func<Task> failure)
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static Task<Result> BindAsync(
        this Result result,
        Func<Task<Result>> bind)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            () => BindStatusSuccessAsync(bind),
            () => Task.FromResult(Result.Failure()));
    }

    public static async Task<Result> BindAsync(
        this Task<Result> source,
        Func<Task<Result>> bind)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<Result> TapAsync(this Result result, Func<Task> action)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => TapStatusAsync(result, action),
            () => Task.FromResult(Result.Failure()));
    }

    public static async Task<Result> TapAsync(
        this Task<Result> source,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result> TapFailureAsync(this Result result, Func<Task> action)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => Task.FromResult(Result.Success()),
            () => TapStatusAsync(result, action));
    }

    public static async Task<Result> TapFailureAsync(
        this Task<Result> source,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapFailureAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result> RecoverWithAsync(
        this Result result,
        Func<Task<Result>> fallback)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            () => Task.FromResult(Result.Success()),
            () => RecoverStatusAsync(fallback));
    }

    public static async Task<Result> RecoverWithAsync(
        this Task<Result> source,
        Func<Task<Result>> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
            .ConfigureAwait(false);
    }

    public static async Task<TResult> Match<TError, TResult>(
        this Task<Result<TError>> source,
        Func<TResult> success,
        Func<TError, TResult> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task Match<TError>(
        this Task<Result<TError>> source,
        Action success,
        Action<TError> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task<Result<TError>> Bind<TError>(
        this Task<Result<TError>> source,
        Func<Result<TError>> bind)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<Result<TValue, TError>> Bind<TValue, TError>(
        this Task<Result<TError>> source,
        Func<Result<TValue, TError>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<Result<TNextError>> MapError<TError, TNextError>(
        this Task<Result<TError>> source,
        Func<TError, TNextError> map)
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    public static async Task<Result<TError>> Tap<TError>(
        this Task<Result<TError>> source,
        Action action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    public static async Task<Result<TError>> TapError<TError>(
        this Task<Result<TError>> source,
        Action<TError> action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    public static async Task<Result<TError>> Recover<TError>(
        this Task<Result<TError>> source,
        Action<TError> fallback)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    public static async Task<Result<TError>> RecoverWith<TError>(
        this Task<Result<TError>> source,
        Func<TError, Result<TError>> fallback)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    public static Task<TResult> MatchAsync<TError, TResult>(
        this Result<TError> result,
        Func<Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            () => RequireTask(success()),
            error => RequireTask(failure(error)));
    }

    public static Task MatchAsync<TError>(
        this Result<TError> result,
        Func<Task> success,
        Func<TError, Task> failure)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            () => RequireTask(success()),
            error => RequireTask(failure(error)));
    }

    public static async Task<TResult> MatchAsync<TError, TResult>(
        this Task<Result<TError>> source,
        Func<Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static async Task MatchAsync<TError>(
        this Task<Result<TError>> source,
        Func<Task> success,
        Func<TError, Task> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static Task<Result<TError>> BindAsync<TError>(
        this Result<TError> result,
        Func<Task<Result<TError>>> bind)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            () => BindErrorSuccessAsync(bind),
            error => Task.FromResult(Result.Failure(error)));
    }

    public static async Task<Result<TError>> BindAsync<TError>(
        this Task<Result<TError>> source,
        Func<Task<Result<TError>>> bind)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<Result<TValue, TError>> BindAsync<TValue, TError>(
        this Result<TError> result,
        Func<Task<Result<TValue, TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            () => BindErrorSuccessAsync(bind),
            error => Task.FromResult(Result.Failure<TValue, TError>(error)));
    }

    public static async Task<Result<TValue, TError>> BindAsync<TValue, TError>(
        this Task<Result<TError>> source,
        Func<Task<Result<TValue, TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<Result<TError>> TapAsync<TError>(
        this Result<TError> result,
        Func<Task> action)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => TapErrorResultAsync(result, action),
            error => Task.FromResult(Result.Failure(error)));
    }

    public static async Task<Result<TError>> TapAsync<TError>(
        this Task<Result<TError>> source,
        Func<Task> action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result<TError>> TapErrorAsync<TError>(
        this Result<TError> result,
        Func<TError, Task> action)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => Task.FromResult(Result.Success<TError>()),
            error => TapErrorResultAsync(result, error, action));
    }

    public static async Task<Result<TError>> TapErrorAsync<TError>(
        this Task<Result<TError>> source,
        Func<TError, Task> action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapErrorAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result<TError>> RecoverWithAsync<TError>(
        this Result<TError> result,
        Func<TError, Task<Result<TError>>> fallback)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            () => Task.FromResult(Result.Success<TError>()),
            error => RecoverErrorAsync(error, fallback));
    }

    public static async Task<Result<TError>> RecoverWithAsync<TError>(
        this Task<Result<TError>> source,
        Func<TError, Task<Result<TError>>> fallback)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
            .ConfigureAwait(false);
    }

    private static async Task<Result> BindStatusSuccessAsync(Func<Task<Result>> bind)
    {
        var result = await RequireTask(bind()).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result> TapStatusAsync(Result result, Func<Task> action)
    {
        await RequireTask(action()).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result> RecoverStatusAsync(Func<Task<Result>> fallback)
    {
        var result = await RequireTask(fallback()).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TError>> BindErrorSuccessAsync<TError>(
        Func<Task<Result<TError>>> bind)
        where TError : notnull
    {
        var result = await RequireTask(bind()).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TValue, TError>> BindErrorSuccessAsync<TValue, TError>(
        Func<Task<Result<TValue, TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        var result = await RequireTask(bind()).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TError>> TapErrorResultAsync<TError>(
        Result<TError> result,
        Func<Task> action)
        where TError : notnull
    {
        await RequireTask(action()).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result<TError>> TapErrorResultAsync<TError>(
        Result<TError> result,
        TError error,
        Func<TError, Task> action)
        where TError : notnull
    {
        await RequireTask(action(error)).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result<TError>> RecoverErrorAsync<TError>(
        TError error,
        Func<TError, Task<Result<TError>>> fallback)
        where TError : notnull
    {
        var result = await RequireTask(fallback(error)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }
}
