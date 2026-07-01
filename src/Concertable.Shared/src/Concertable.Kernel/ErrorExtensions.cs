using FluentResults;

namespace Concertable.Kernel;

public static class ErrorExtensions
{
    public static IEnumerable<string> SelectMessages(this IEnumerable<IError> errors)
        => errors.Select(e => e.Message);
}
