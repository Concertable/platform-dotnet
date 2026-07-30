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
            DependencyUnavailableException => CreateDependencyProblemDetails(
                HttpStatusCode.ServiceUnavailable,
                "A required service is temporarily unavailable.",
                "dependency.unavailable"),
            DependencyTimeoutException => CreateDependencyProblemDetails(
                HttpStatusCode.GatewayTimeout,
                "A required service did not respond in time.",
                "dependency.timeout"),
            _ => CreateInternalServerError(exception)
        };

        logger.UnhandledException(exception);

        await ApplicationProblemDetails
            .WriteAsync(
                httpContext,
                problemDetailsService,
                problemDetails,
                exception)
            .ConfigureAwait(false);

        return true;
    }

    private static ProblemDetails CreateValidationProblemDetails(BadRequestException exception)
    {
        var problemDetails = ApplicationProblemDetails.Create(
            exception.StatusCode,
            exception.Title,
            exception.Message);
        problemDetails.Extensions[ApplicationProblemDetails.ErrorsExtensionKey] =
            exception.ValidationErrors!;
        return problemDetails;
    }

    private static ProblemDetails CreateDependencyProblemDetails(
        HttpStatusCode statusCode,
        string detail,
        string code)
    {
        var problemDetails = ApplicationProblemDetails.Create(statusCode, detail);
        problemDetails.Extensions[ApplicationProblemDetails.CodeExtensionKey] = code;
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
