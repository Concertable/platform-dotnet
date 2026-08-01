namespace Concertable.Kernel.Functional;

public static class ResultCollectionExtensions
{
    public static Result<IReadOnlyList<TValue>, TError> Sequence<TValue, TError>(
        this IEnumerable<Result<TValue, TError>> source)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var values = new List<TValue>();

        foreach (var result in source)
        {
            if (result.TryGetError(out var error))
                return Result.Failure<IReadOnlyList<TValue>, TError>(error);

            if (!result.TryGetValue(out var value))
                throw new InvalidOperationException("A successful Result must contain a value.");

            values.Add(value);
        }

        return Result.Success<IReadOnlyList<TValue>, TError>(values);
    }

    public static Result<IReadOnlyList<TValue>, TError> Traverse<TSource, TValue, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, Result<TValue, TError>> selector)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new List<TValue>();

        foreach (var item in source)
        {
            var result = selector(item);

            if (result.TryGetError(out var error))
                return Result.Failure<IReadOnlyList<TValue>, TError>(error);

            if (!result.TryGetValue(out var value))
                throw new InvalidOperationException("A successful Result must contain a value.");

            values.Add(value);
        }

        return Result.Success<IReadOnlyList<TValue>, TError>(values);
    }

    public static async Task<Result<IReadOnlyList<TValue>, TError>> TraverseAsync<TSource, TValue, TError>(
        this IEnumerable<TSource> source,
        Func<TSource, CancellationToken, Task<Result<TValue, TError>>> selector,
        CancellationToken cancellationToken = default)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        cancellationToken.ThrowIfCancellationRequested();

        var values = new List<TValue>();

        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var task = selector(item, cancellationToken);
            ArgumentNullException.ThrowIfNull(task);
            var result = await task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (result.TryGetError(out var error))
                return Result.Failure<IReadOnlyList<TValue>, TError>(error);

            if (!result.TryGetValue(out var value))
                throw new InvalidOperationException("A successful Result must contain a value.");

            values.Add(value);
        }

        return Result.Success<IReadOnlyList<TValue>, TError>(values);
    }

    public static Result<Unit, TError> Combine<TError>(
        this IEnumerable<Result<Unit, TError>> source)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var result in source)
        {
            if (result.TryGetError(out var error))
                return Result.Failure<Unit, TError>(error);
        }

        return Result.Success<TError>();
    }
}
