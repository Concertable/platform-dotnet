using CSharpFunctionalExtensions;

namespace Concertable.Kernel.Extensions;

public static class MaybeResultExtensions
{
    public static Result<T, TError> OrFailure<T, TError>(
        this Maybe<T> maybe,
        TError error)
        where TError : notnull =>
        maybe.ToResult(error);

    public static Result<T, TError> OrFailure<T, TError>(
        this Maybe<T> maybe,
        Func<TError> errorFactory)
        where TError : notnull =>
        maybe.ToResult(errorFactory);
}
