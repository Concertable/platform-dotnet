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

    private static GlobalExceptionHandler CreateHandler(string environmentName)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddProblemDetails();
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
