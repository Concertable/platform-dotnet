namespace Concertable.Kernel.Functional;

public static partial class ResultTaskExtensions
{
    public static async Task<TResult> Match<TResult>(
        this Task<Result> source,
        Func<TResult> success,
        Func<string, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task Match(
        this Task<Result> source,
        Action success,
        Action<string> failure)
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

    public static async Task<UnitResult<TError>> Bind<TError>(
        this Task<Result> source,
        Func<UnitResult<TError>> bind,
        Func<string, TError> mapError)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    public static async Task<Result<TValue, TError>> Bind<TValue, TError>(
        this Task<Result> source,
        Func<Result<TValue, TError>> bind,
        Func<string, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    public static async Task<UnitResult<TError>> MapError<TError>(
        this Task<Result> source,
        Func<string, TError> map)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    public static async Task<Result> Tap(this Task<Result> source, Action action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    public static async Task<Result> TapError(this Task<Result> source, Action<string> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    public static async Task<Result> Recover(this Task<Result> source, Action<string> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    public static async Task<Result> RecoverWith(
        this Task<Result> source,
        Func<string, Result> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    public static Task<TResult> MatchAsync<TResult>(
        this Result result,
        Func<Task<TResult>> success,
        Func<string, Task<TResult>> failure)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            () => RequireTask(success()),
            error => RequireTask(failure(error)));
    }

    public static Task MatchAsync(
        this Result result,
        Func<Task> success,
        Func<string, Task> failure)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            () => RequireTask(success()),
            error => RequireTask(failure(error)));
    }

    public static async Task<TResult> MatchAsync<TResult>(
        this Task<Result> source,
        Func<Task<TResult>> success,
        Func<string, Task<TResult>> failure)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static async Task MatchAsync(
        this Task<Result> source,
        Func<Task> success,
        Func<string, Task> failure)
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
            error => Task.FromResult(Result.Failure(error)));
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
            error => Task.FromResult(Result.Failure(error)));
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

    public static Task<Result> TapErrorAsync(this Result result, Func<string, Task> action)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => Task.FromResult(Result.Success()),
            error => TapStatusErrorAsync(result, error, action));
    }

    public static async Task<Result> TapErrorAsync(
        this Task<Result> source,
        Func<string, Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapErrorAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result> RecoverWithAsync(
        this Result result,
        Func<string, Task<Result>> fallback)
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            () => Task.FromResult(Result.Success()),
            error => RecoverStatusAsync(error, fallback));
    }

    public static async Task<Result> RecoverWithAsync(
        this Task<Result> source,
        Func<string, Task<Result>> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
            .ConfigureAwait(false);
    }

    public static async Task<TResult> Match<TError, TResult>(
        this Task<UnitResult<TError>> source,
        Func<TResult> success,
        Func<TError, TResult> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task Match<TError>(
        this Task<UnitResult<TError>> source,
        Action success,
        Action<TError> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task<UnitResult<TError>> Bind<TError>(
        this Task<UnitResult<TError>> source,
        Func<UnitResult<TError>> bind)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<Result<TValue, TError>> Bind<TValue, TError>(
        this Task<UnitResult<TError>> source,
        Func<Result<TValue, TError>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<UnitResult<TNextError>> MapError<TError, TNextError>(
        this Task<UnitResult<TError>> source,
        Func<TError, TNextError> map)
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    public static async Task<UnitResult<TError>> Tap<TError>(
        this Task<UnitResult<TError>> source,
        Action action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    public static async Task<UnitResult<TError>> TapError<TError>(
        this Task<UnitResult<TError>> source,
        Action<TError> action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    public static async Task<UnitResult<TError>> Recover<TError>(
        this Task<UnitResult<TError>> source,
        Action<TError> fallback)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    public static async Task<UnitResult<TError>> RecoverWith<TError>(
        this Task<UnitResult<TError>> source,
        Func<TError, UnitResult<TError>> fallback)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    public static Task<TResult> MatchAsync<TError, TResult>(
        this UnitResult<TError> result,
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
        this UnitResult<TError> result,
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
        this Task<UnitResult<TError>> source,
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
        this Task<UnitResult<TError>> source,
        Func<Task> success,
        Func<TError, Task> failure)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static Task<UnitResult<TError>> BindAsync<TError>(
        this UnitResult<TError> result,
        Func<Task<UnitResult<TError>>> bind)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            () => BindErrorSuccessAsync(bind),
            error => Task.FromResult(UnitResult.Failure(error)));
    }

    public static async Task<UnitResult<TError>> BindAsync<TError>(
        this Task<UnitResult<TError>> source,
        Func<Task<UnitResult<TError>>> bind)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<Result<TValue, TError>> BindAsync<TValue, TError>(
        this UnitResult<TError> result,
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
        this Task<UnitResult<TError>> source,
        Func<Task<Result<TValue, TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<UnitResult<TError>> TapAsync<TError>(
        this UnitResult<TError> result,
        Func<Task> action)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => TapErrorResultAsync(result, action),
            error => Task.FromResult(UnitResult.Failure(error)));
    }

    public static async Task<UnitResult<TError>> TapAsync<TError>(
        this Task<UnitResult<TError>> source,
        Func<Task> action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<UnitResult<TError>> TapErrorAsync<TError>(
        this UnitResult<TError> result,
        Func<TError, Task> action)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            () => Task.FromResult(UnitResult.Success<TError>()),
            error => TapErrorResultAsync(result, error, action));
    }

    public static async Task<UnitResult<TError>> TapErrorAsync<TError>(
        this Task<UnitResult<TError>> source,
        Func<TError, Task> action)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapErrorAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<UnitResult<TError>> RecoverWithAsync<TError>(
        this UnitResult<TError> result,
        Func<TError, Task<UnitResult<TError>>> fallback)
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            () => Task.FromResult(UnitResult.Success<TError>()),
            error => RecoverErrorAsync(error, fallback));
    }

    public static async Task<UnitResult<TError>> RecoverWithAsync<TError>(
        this Task<UnitResult<TError>> source,
        Func<TError, Task<UnitResult<TError>>> fallback)
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

    private static async Task<Result> TapStatusErrorAsync(
        Result result,
        string error,
        Func<string, Task> action)
    {
        await RequireTask(action(error)).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result> RecoverStatusAsync(
        string error,
        Func<string, Task<Result>> fallback)
    {
        var result = await RequireTask(fallback(error)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<UnitResult<TError>> BindErrorSuccessAsync<TError>(
        Func<Task<UnitResult<TError>>> bind)
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

    private static async Task<UnitResult<TError>> TapErrorResultAsync<TError>(
        UnitResult<TError> result,
        Func<Task> action)
        where TError : notnull
    {
        await RequireTask(action()).ConfigureAwait(false);
        return result;
    }

    private static async Task<UnitResult<TError>> TapErrorResultAsync<TError>(
        UnitResult<TError> result,
        TError error,
        Func<TError, Task> action)
        where TError : notnull
    {
        await RequireTask(action(error)).ConfigureAwait(false);
        return result;
    }

    private static async Task<UnitResult<TError>> RecoverErrorAsync<TError>(
        TError error,
        Func<TError, Task<UnitResult<TError>>> fallback)
        where TError : notnull
    {
        var result = await RequireTask(fallback(error)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }
}
