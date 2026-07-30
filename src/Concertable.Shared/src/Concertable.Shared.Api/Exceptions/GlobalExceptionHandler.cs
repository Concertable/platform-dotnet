using Concertable.Kernel;
using Concertable.Kernel.Exceptions;
using Concertable.Shared.Api.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Concertable.Shared.Api.Exceptions;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;
    private readonly IHostEnvironment environment;
    private readonly IProblemDetailsService problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment,
        IProblemDetailsService problemDetailsService)
    {
        this.logger = logger;
        this.environment = environment;
        this.problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
            return false;

        var problemDetails = exception switch
        {
            UnauthorizedAccessException unauthorized => ApplicationProblemDetails.Create(
                HttpStatusCode.Unauthorized,
                unauthorized.Message),
            DomainException domain => ApplicationProblemDetails.Create(
                HttpStatusCode.BadRequest,
                domain.Message),
            BadRequestException { ValidationErrors: not null } badRequest => CreateValidationProblemDetails(badRequest),
            HttpException http => ApplicationProblemDetails.Create(
                http.StatusCode,
                http.Title,
                http.Message),
            _ => CreateInternalServerError(exception)
        };

        problemDetails.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        logger.UnhandledException(exception);

        await problemDetailsService
            .WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            })
            .ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails CreateValidationProblemDetails(BadRequestException exception)
    {
        var problemDetails = ApplicationProblemDetails.Create(
            exception.StatusCode,
            exception.Title,
            exception.Message);
        problemDetails.Extensions["errors"] = exception.ValidationErrors!;
        return problemDetails;
    }

    private ProblemDetails CreateInternalServerError(Exception exception)
    {
        var problemDetails = ApplicationProblemDetails.Create(
            HttpStatusCode.InternalServerError,
            environment.IsProduction()
                ? "An unexpected error occurred."
                : exception.Message);

        if (environment.IsProduction())
            return problemDetails;

        problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
        problemDetails.Extensions["stackTrace"] = exception.ToString();

        if (exception.InnerException is not null)
            problemDetails.Extensions["innerException"] = exception.InnerException.ToString();

        return problemDetails;
    }
}
