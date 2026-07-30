using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Concertable.Shared.Api.Http;

internal static class ApplicationProblemDetails
{
    internal static ProblemDetails Create(HttpStatusCode statusCode, string detail) =>
        Create(
            statusCode,
            statusCode.ToReasonPhrase(),
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
