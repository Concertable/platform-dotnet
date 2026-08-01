using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Frozen;
using System.Net;

namespace Concertable.Shared.Api.Results;

internal static class ErrorHttpExtensions
{
    private static readonly FrozenDictionary<ErrorKind, (HttpStatusCode StatusCode, string Title)> httpMappings =
        new Dictionary<ErrorKind, (HttpStatusCode StatusCode, string Title)>
        {
            [ErrorKind.Invalid] = (HttpStatusCode.BadRequest, "Bad Request"),
            [ErrorKind.NotFound] = (HttpStatusCode.NotFound, "Not Found"),
            [ErrorKind.Conflict] = (HttpStatusCode.Conflict, "Conflict"),
            [ErrorKind.Unauthenticated] = (HttpStatusCode.Unauthorized, "Unauthorized"),
            [ErrorKind.Forbidden] = (HttpStatusCode.Forbidden, "Forbidden"),
            [ErrorKind.PaymentRequired] = (HttpStatusCode.PaymentRequired, "Payment Required")
        }.ToFrozenDictionary();

    internal static ApplicationErrorResult ToProblemActionResult<TError>(this TError error)
        where TError : IError
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        var definition = error.Definition
            ?? throw new InvalidOperationException("An error definition is required.");
        var (statusCode, title) = httpMappings[definition.Kind];
        var problemDetails = CreateProblemDetails(definition, statusCode, title);
        problemDetails.Extensions[ApplicationProblemDetails.CodeExtensionKey] = definition.Code;

        return new ApplicationErrorResult(problemDetails);
    }

    private static ProblemDetails CreateProblemDetails(
        ErrorDefinition definition,
        HttpStatusCode statusCode,
        string title) =>
        definition is ValidationErrorDefinition validation
            ? new ValidationProblemDetails(
                validation.Errors.ToDictionary(
                    error => error.Key,
                    error => error.Value.ToArray()))
            {
                Status = (int)statusCode,
                Title = title,
                Detail = definition.Message
            }
            : new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = definition.Message
            };
}
