using Concertable.Kernel;
using Concertable.Kernel.Exceptions;
using Concertable.Shared.Api.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace Concertable.Shared.Api.UnitTests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_Cancellation_PassesThrough()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_ProductionFault_ReturnsSafeProblemDetails()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sensitive detail."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.InternalServerError),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred.", problemDetails.GetProperty("detail").GetString());
        Assert.Equal(
            context.TraceIdentifier,
            problemDetails.GetProperty("traceId").GetString());
        Assert.False(problemDetails.TryGetProperty("exceptionType", out _));
        Assert.False(problemDetails.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task TryHandleAsync_DevelopmentFault_ReturnsDiagnosticDetails()
    {
        var handler = CreateHandler(Environments.Development);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Diagnostic detail."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal("Diagnostic detail.", problemDetails.GetProperty("detail").GetString());
        Assert.Equal(
            typeof(InvalidOperationException).FullName,
            problemDetails.GetProperty("exceptionType").GetString());
        Assert.Contains("Diagnostic detail.", problemDetails.GetProperty("stackTrace").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_UnauthorizedAccessException_PreservesStatusAndDetail()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new UnauthorizedAccessException("Authentication is required."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.Unauthorized),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal("Authentication is required.", problemDetails.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_DomainException_PreservesStatusAndDetail()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new DomainException("The operation violates a domain rule."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.BadRequest),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal(
            "The operation violates a domain rule.",
            problemDetails.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_LegacyValidationException_PreservesErrors()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new BadRequestException(["First error.", "Second error."]),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        var errors = problemDetails.GetProperty("errors").EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.BadRequest),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal(["First error.", "Second error."], errors);
    }

    [Fact]
    public async Task TryHandleAsync_LegacyHttpException_PreservesStatusAndDetail()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new NotFoundException("Concert not found."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.NotFound),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal("Concert not found.", problemDetails.GetProperty("detail").GetString());
        Assert.Equal("/test", problemDetails.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_DependencyUnavailable_ReturnsSafeServiceUnavailable()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new DependencyUnavailableException(
                "Payment",
                new InvalidOperationException("Sensitive provider detail.")),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.ServiceUnavailable),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal(
            "A required service is temporarily unavailable.",
            problemDetails.GetProperty("detail").GetString());
        Assert.Equal(
            "dependency.unavailable",
            problemDetails.GetProperty("code").GetString());
        Assert.DoesNotContain(
            "Sensitive provider detail.",
            problemDetails.GetRawText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandleAsync_DependencyTimeout_ReturnsSafeGatewayTimeout()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new DependencyTimeoutException(
                "Payment",
                new TimeoutException("Sensitive provider detail.")),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, context.Response.StatusCode);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)HttpStatusCode.GatewayTimeout),
            problemDetails.GetProperty("title").GetString());
        Assert.Equal(
            "A required service did not respond in time.",
            problemDetails.GetProperty("detail").GetString());
        Assert.Equal(
            "dependency.timeout",
            problemDetails.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_UnclassifiedTimeout_ReturnsSafeInternalServerError()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new TimeoutException("Sensitive provider detail."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(
            "An unexpected error occurred.",
            problemDetails.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_Customizer_AppliesToExceptionProblemDetails()
    {
        var handler = CreateHandler(
            Environments.Production,
            options => options.CustomizeProblemDetails = problemContext =>
            {
                problemContext.ProblemDetails.Extensions["customized"] = true;
                problemContext.ProblemDetails.Extensions["hasException"] =
                    problemContext.Exception is not null;
            });
        var context = CreateContext();

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Sensitive detail."),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.True(problemDetails.GetProperty("customized").GetBoolean());
        Assert.True(problemDetails.GetProperty("hasException").GetBoolean());
    }

    [Fact]
    public async Task TryHandleAsync_UnsupportedAccept_SerializesSelectedProblemDetails()
    {
        var handler = CreateHandler(Environments.Production);
        var context = CreateContext();
        context.Request.Headers.Accept = MediaTypeNames.Application.Xml;

        var handled = await handler.TryHandleAsync(
            context,
            new DependencyUnavailableException(
                "Payment",
                new InvalidOperationException("Sensitive provider detail.")),
            CancellationToken.None);

        var problemDetails = await ReadResponseAsync(context);
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.ProblemJson, context.Response.ContentType);
        Assert.Equal(
            "A required service is temporarily unavailable.",
            problemDetails.GetProperty("detail").GetString());
        Assert.Equal(
            "dependency.unavailable",
            problemDetails.GetProperty("code").GetString());
        Assert.Equal("/test", problemDetails.GetProperty("instance").GetString());
        Assert.Equal(
            context.TraceIdentifier,
            problemDetails.GetProperty("traceId").GetString());
    }

    private static GlobalExceptionHandler CreateHandler(
        string environmentName,
        Action<ProblemDetailsOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddProblemDetails(options => configure?.Invoke(options));
        var provider = services.BuildServiceProvider();
        return new(
            NullLogger<GlobalExceptionHandler>.Instance,
            new TestHostEnvironment
            {
                EnvironmentName = environmentName
            },
            provider.GetRequiredService<IProblemDetailsService>());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.TraceIdentifier = "trace-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = nameof(GlobalExceptionHandlerTests);
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = null!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
