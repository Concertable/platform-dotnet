using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Http;
using System.Collections.Frozen;
using System.Net;

namespace Concertable.Shared.Api.Results;

internal static class ErrorHttpExtensions
{
    private const string CodeExtensionKey = "code";
    private const string ErrorsExtensionKey = "errors";

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
        var descriptor = error.Descriptor;
        var statusCode = httpStatusCodes[descriptor.Kind];
        var problemDetails = ApplicationProblemDetails.Create(statusCode, descriptor.Message);
        problemDetails.Extensions[CodeExtensionKey] = descriptor.Code;

        if (descriptor is ValidationErrorDescriptor validation)
            problemDetails.Extensions[ErrorsExtensionKey] = validation.Errors;

        return new ApplicationErrorResult(problemDetails);
    }
}
