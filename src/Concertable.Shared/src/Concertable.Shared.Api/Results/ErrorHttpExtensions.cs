using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Http;
using Microsoft.AspNetCore.Mvc;
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

    internal static ApplicationErrorResult ToProblemActionResult<TError>(this TError error)
        where TError : IError
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        var definition = error.Definition
            ?? throw new InvalidOperationException("An error definition is required.");
        var statusCode = httpStatusCodes[definition.Kind];
        var problemDetails = CreateProblemDetails(definition, statusCode);
        problemDetails.Extensions[ApplicationProblemDetails.CodeExtensionKey] = definition.Code;

        return new ApplicationErrorResult(problemDetails);
    }

    private static ProblemDetails CreateProblemDetails(
        ErrorDefinition definition,
        HttpStatusCode statusCode) =>
        definition is ValidationErrorDefinition validation
            ? new ValidationProblemDetails(
                validation.Errors.ToDictionary(
                    error => error.Key,
                    error => error.Value.ToArray()))
            {
                Status = (int)statusCode,
                Title = statusCode.ToReasonPhrase(),
                Detail = definition.Message
            }
            : new ProblemDetails
            {
                Status = (int)statusCode,
                Title = statusCode.ToReasonPhrase(),
                Detail = definition.Message
            };
}
