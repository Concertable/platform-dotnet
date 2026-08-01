namespace Concertable.Kernel.Functional;

public static partial class ResultTaskExtensions
{
    public static async Task<TResult> Match<TValue, TError, TResult>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, TResult> success,
        Func<TError, TResult> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task Match<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Action<TValue> success,
        Action<TError> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(success, failure);
    }

    public static async Task<Result<TNext, TError>> Map<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, TNext> map)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Map(map);
    }

    public static async Task<Result<TNext, TError>> Bind<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Result<TNext, TError>> bind)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<UnitResult<TError>> Bind<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, UnitResult<TError>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    public static async Task<Result<TValue, TNextError>> MapError<TValue, TError, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, TNextError> map)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    public static async Task<Result<TValue, TError>> Ensure<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, bool> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Ensure(predicate, errorFactory);
    }

    public static async Task<Result<TValue, TError>> Tap<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Action<TValue> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    public static async Task<Result<TValue, TError>> TapError<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Action<TError> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    public static async Task<Result<TValue, TError>> Recover<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, TValue> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    public static async Task<Result<TValue, TError>> RecoverWith<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Result<TValue, TError>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    public static Task<TResult> MatchAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            value => RequireTask(success(value)),
            error => RequireTask(failure(error)));
    }

    public static Task MatchAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task> success,
        Func<TError, Task> failure)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            value => RequireTask(success(value)),
            error => RequireTask(failure(error)));
    }

    public static async Task<TResult> MatchAsync<TValue, TError, TResult>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static async Task MatchAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task> success,
        Func<TError, Task> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    public static Task<Result<TNext, TError>> MapAsync<TValue, TError, TNext>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return result.Match(
            value => MapSuccessAsync<TValue, TNext, TError>(value, map),
            error => Task.FromResult(Result.Failure<TNext, TError>(error)));
    }

    public static async Task<Result<TNext, TError>> MapAsync<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MapAsync(map)
            .ConfigureAwait(false);
    }

    public static Task<Result<TNext, TError>> BindAsync<TValue, TError, TNext>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TNext, TError>>> bind)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(Result.Failure<TNext, TError>(error)));
    }

    public static async Task<Result<TNext, TError>> BindAsync<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<Result<TNext, TError>>> bind)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<UnitResult<TError>> BindAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<UnitResult<TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(UnitResult.Failure(error)));
    }

    public static async Task<UnitResult<TError>> BindAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<UnitResult<TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    public static Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<bool>> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        return result.Match(
            value => EnsureSuccessAsync(value, predicate, errorFactory),
            error => Task.FromResult(Result.Failure<TValue, TError>(error)));
    }

    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<bool>> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .EnsureAsync(predicate, errorFactory)
            .ConfigureAwait(false);
    }

    public static Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            value => TapSuccessAsync(result, value, action),
            error => Task.FromResult(Result.Failure<TValue, TError>(error)));
    }

    public static async Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result<TValue, TError>> TapErrorAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            value => Task.FromResult(Result.Success<TValue, TError>(value)),
            error => TapFailureAsync(result, error, action));
    }

    public static async Task<Result<TValue, TError>> TapErrorAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapErrorAsync(action)
            .ConfigureAwait(false);
    }

    public static Task<Result<TValue, TError>> RecoverWithAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            value => Task.FromResult(Result.Success<TValue, TError>(value)),
            error => RecoverFailureAsync(error, fallback));
    }

    public static async Task<Result<TValue, TError>> RecoverWithAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
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

    private static async Task<Result<TNext, TError>> MapSuccessAsync<TValue, TNext, TError>(
        TValue value,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TNext : notnull
        where TError : notnull =>
        Result.Success<TNext, TError>(
            await RequireTask(map(value)).ConfigureAwait(false));

    private static async Task<Result<TNext, TError>> BindSuccessAsync<TValue, TNext, TError>(
        TValue value,
        Func<TValue, Task<Result<TNext, TError>>> bind)
        where TValue : notnull
        where TNext : notnull
        where TError : notnull
    {
        var result = await RequireTask(bind(value)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<UnitResult<TError>> BindSuccessAsync<TValue, TError>(
        TValue value,
        Func<TValue, Task<UnitResult<TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        var result = await RequireTask(bind(value)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TValue, TError>> EnsureSuccessAsync<TValue, TError>(
        TValue value,
        Func<TValue, Task<bool>> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull =>
        await RequireTask(predicate(value)).ConfigureAwait(false)
            ? Result.Success<TValue, TError>(value)
            : Result.Failure<TValue, TError>(errorFactory());

    private static async Task<Result<TValue, TError>> TapSuccessAsync<TValue, TError>(
        Result<TValue, TError> result,
        TValue value,
        Func<TValue, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        await RequireTask(action(value)).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result<TValue, TError>> TapFailureAsync<TValue, TError>(
        Result<TValue, TError> result,
        TError error,
        Func<TError, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        await RequireTask(action(error)).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result<TValue, TError>> RecoverFailureAsync<TValue, TError>(
        TError error,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        var result = await RequireTask(fallback(error)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }
}
