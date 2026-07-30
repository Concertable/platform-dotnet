using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Concertable.Shared.Api.Http;

internal static class ApplicationProblemDetails
{
    internal static ProblemDetails Create(HttpStatusCode statusCode, string detail) =>
        Create(
            statusCode,
            ReasonPhrases.GetReasonPhrase((int)statusCode),
            detail);

    internal static ProblemDetails Create(
        HttpStatusCode statusCode,
        string title,
        string detail) =>
        new()
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail
        };
}
