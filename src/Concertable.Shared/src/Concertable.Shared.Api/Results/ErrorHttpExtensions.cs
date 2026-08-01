using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Http;
using System.Collections.Frozen;
using System.Net;

namespace Concertable.Shared.Api.Results;

internal static class ErrorHttpExtensions
{
    private static readonly FrozenDictionary<ErrorKind, HttpStatusCode> httpStatusCodes =
        new Dictionary<ErrorKind, HttpStatusCode>
        {
            [ErrorKind.Invalid] = HttpStatusCode.BadRequest,
            [ErrorKind.NotFound] = HttpStatusCode.NotFound,
            [ErrorKind.Conflict] = HttpStatusCode.Conflict,
            [ErrorKind.Unauthenticated] = HttpStatusCode.Unauthorized,
            [ErrorKind.Forbidden] = HttpStatusCode.Forbidden,
            [ErrorKind.PaymentRequired] = HttpStatusCode.PaymentRequired
        }.ToFrozenDictionary();

    internal static ApplicationErrorResult ToProblemActionResult(this IError error)
    {
        var definition = error.Definition;
        var statusCode = httpStatusCodes[definition.Kind];
        var problemDetails = ApplicationProblemDetails.Create(statusCode, definition.Message);
        problemDetails.Extensions[ApplicationProblemDetails.CodeExtensionKey] = definition.Code;

        if (definition is ValidationErrorDefinition validation)
            problemDetails.Extensions[ApplicationProblemDetails.ErrorsExtensionKey] = validation.Errors;

        return new ApplicationErrorResult(problemDetails);
    }
}
