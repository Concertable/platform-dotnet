using Concertable.Shared.Api.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Mime;

namespace Concertable.Shared.Api.Results;

internal sealed class ApplicationErrorResult : ObjectResult
{
    public ApplicationErrorResult(ProblemDetails problemDetails)
        : base(problemDetails)
    {
        StatusCode = problemDetails.Status;
        ContentTypes.Add(MediaTypeNames.Application.ProblemJson);
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var problemDetails = (ProblemDetails)Value!;
        var problemDetailsService = context.HttpContext.RequestServices
            .GetRequiredService<IProblemDetailsService>();

        await ApplicationProblemDetails
            .WriteAsync(
                context.HttpContext,
                problemDetailsService,
                problemDetails)
            .ConfigureAwait(false);
    }
}
