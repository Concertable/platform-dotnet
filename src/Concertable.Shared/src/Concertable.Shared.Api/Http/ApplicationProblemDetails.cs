using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace Concertable.Shared.Api.Http;

internal static class ApplicationProblemDetails
{
    internal const string CodeExtensionKey = "code";
    internal const string ErrorsExtensionKey = "errors";
    internal const string TraceIdExtensionKey = "traceId";

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

    internal static async Task WriteAsync(
        HttpContext httpContext,
        IProblemDetailsService problemDetailsService,
        ProblemDetails problemDetails,
        Exception? exception = null)
    {
        var statusCode = problemDetails.Status
            ?? throw new InvalidOperationException("ProblemDetails status is required.");
        problemDetails.Instance = httpContext.Request.PathBase.Add(httpContext.Request.Path);
        problemDetails.Extensions[TraceIdExtensionKey] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = statusCode;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        };

        if (await problemDetailsService.TryWriteAsync(context).ConfigureAwait(false))
            return;

        await httpContext.Response
            .WriteAsJsonAsync(
                problemDetails,
                JsonSerializerOptions.Web,
                MediaTypeNames.Application.ProblemJson,
                httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
