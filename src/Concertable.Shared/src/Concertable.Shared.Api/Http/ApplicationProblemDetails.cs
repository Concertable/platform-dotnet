using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Concertable.Shared.Api.Http;

internal static class ApplicationProblemDetails
{
    internal static ProblemDetails Create(int statusCode, string detail) =>
        Create(
            statusCode,
            ReasonPhrases.GetReasonPhrase(statusCode),
            detail);

    internal static ProblemDetails Create(HttpStatusCode statusCode, string detail) =>
        Create((int)statusCode, detail);

    internal static ProblemDetails Create(
        HttpStatusCode statusCode,
        string title,
        string detail) =>
        Create((int)statusCode, title, detail);

    private static ProblemDetails Create(int statusCode, string title, string detail) =>
        new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
}
