using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Http;
using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;

namespace Concertable.Shared.Api.Results;

internal static class ErrorHttpExtensions
{
    private const string CodeExtensionKey = "code";
    private const string ErrorsExtensionKey = "errors";

    private static readonly FrozenDictionary<ErrorKind, int> httpStatusCodes =
        new Dictionary<ErrorKind, int>
        {
            [ErrorKind.Invalid] = StatusCodes.Status400BadRequest,
            [ErrorKind.NotFound] = StatusCodes.Status404NotFound,
            [ErrorKind.Conflict] = StatusCodes.Status409Conflict,
            [ErrorKind.Unauthenticated] = StatusCodes.Status401Unauthorized,
            [ErrorKind.Forbidden] = StatusCodes.Status403Forbidden,
            [ErrorKind.PaymentRequired] = StatusCodes.Status402PaymentRequired
        }.ToFrozenDictionary();

    internal static ApplicationErrorResult ToProblemActionResult(this IError error)
    {
        var descriptor = error.Descriptor;
        var statusCode = httpStatusCodes[descriptor.Kind];
        var problemDetails = ApplicationProblemDetails.Create(statusCode, descriptor.Message);
        problemDetails.Extensions[CodeExtensionKey] = descriptor.Code;

        if (descriptor is ValidationErrorDescriptor validation)
            problemDetails.Extensions[ErrorsExtensionKey] = validation.Errors;

        return new ApplicationErrorResult(problemDetails);
    }
}
