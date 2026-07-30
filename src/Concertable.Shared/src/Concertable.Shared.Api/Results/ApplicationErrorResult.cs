using Microsoft.AspNetCore.Mvc;
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

    public override Task ExecuteResultAsync(ActionContext context)
    {
        var problemDetails = (ProblemDetails)Value!;
        problemDetails.Instance = context.HttpContext.Request.Path;
        return base.ExecuteResultAsync(context);
    }
}
