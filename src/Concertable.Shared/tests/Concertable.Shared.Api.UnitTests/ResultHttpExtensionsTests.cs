using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;
using Concertable.Shared.Api.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace Concertable.Shared.Api.UnitTests;

public sealed class ResultHttpExtensionsTests
{
    [Theory]
    [InlineData(ErrorKind.Invalid, HttpStatusCode.BadRequest, "Bad Request")]
    [InlineData(ErrorKind.NotFound, HttpStatusCode.NotFound, "Not Found")]
    [InlineData(ErrorKind.Conflict, HttpStatusCode.Conflict, "Conflict")]
    [InlineData(ErrorKind.Unauthenticated, HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(ErrorKind.Forbidden, HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(ErrorKind.PaymentRequired, HttpStatusCode.PaymentRequired, "Payment Required")]
    public void ToOkActionResult_FailedResult_MapsSemanticKind(
        ErrorKind kind,
        HttpStatusCode expectedStatus,
        string expectedTitle)
    {
        var error = new TestError(new ErrorDefinition("test.code", "Safe detail.", kind));
        var result = Result.Failure<string, TestError>(error);

        var actionResult = result.ToOkActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal((int)expectedStatus, objectResult.StatusCode);
        Assert.Equal((int)expectedStatus, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal("Safe detail.", problemDetails.Detail);
        Assert.Null(problemDetails.Instance);
        Assert.Equal("test.code", problemDetails.Extensions["code"]);
        Assert.Contains(MediaTypeNames.Application.ProblemJson, objectResult.ContentTypes);
    }

    [Fact]
    public void ToOkActionResult_ValidationFailure_PreservesStructuredErrors()
    {
        IReadOnlyDictionary<string, string[]> validationErrors =
            new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be positive."]
            };
        var error = new TestError(
            new ValidationErrorDefinition(
                "ticket.purchase_invalid",
                "The ticket purchase is invalid.",
                validationErrors));
        var result = Result.Failure<string, TestError>(error);

        var actionResult = result.ToOkActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(validationErrors["quantity"], problemDetails.Errors["quantity"]);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Bad Request", problemDetails.Title);
        Assert.Equal("The ticket purchase is invalid.", problemDetails.Detail);
        Assert.Equal("ticket.purchase_invalid", problemDetails.Extensions["code"]);
    }

    [Fact]
    public async Task ToOkActionResult_ValidationFailureExecution_PreservesStructuredErrors()
    {
        var error = new TestError(
            ErrorDefinition.Validation(
                "ticket.purchase_invalid",
                "The ticket purchase is invalid.",
                new Dictionary<string, string[]>
                {
                    ["quantity"] = ["Quantity must be positive."],
                    ["concert"] = ["Concert is not available."]
                }));
        var result = Result.Failure<string, TestError>(error);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddControllers();
        serviceCollection.AddProblemDetails(
            options => options.CustomizeProblemDetails = problemContext =>
                problemContext.ProblemDetails.Extensions["customized"] = true);
        var services = serviceCollection.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response =
            {
                Body = new MemoryStream()
            }
        };
        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor());

        var actionResult = result.ToOkActionResult();
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        await objectResult.ExecuteResultAsync(actionContext);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var response = document.RootElement;
        var errors = response.GetProperty("errors");
        Assert.Equal(
            "Quantity must be positive.",
            errors.GetProperty("quantity")[0].GetString());
        Assert.Equal(
            "Concert is not available.",
            errors.GetProperty("concert")[0].GetString());
        Assert.Equal("ticket.purchase_invalid", response.GetProperty("code").GetString());
        Assert.True(response.GetProperty("customized").GetBoolean());
        Assert.Equal(MediaTypeNames.Application.ProblemJson, context.Response.ContentType);
    }

    [Fact]
    public void ToOkActionResult_AllErrorKinds_HaveHttpMappings()
    {
        foreach (var kind in Enum.GetValues<ErrorKind>())
        {
            var error = new TestError(new ErrorDefinition("test.code", "Safe detail.", kind));
            var result = Result.Failure<string, TestError>(error);

            var exception = Record.Exception(() => result.ToOkActionResult());

            Assert.Null(exception);
        }
    }

    [Fact]
    public void ToOkActionResult_Success_ReturnsValue()
    {
        var result = Result.Success<string, TestError>("value");

        var actionResult = result.ToOkActionResult();

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal("value", okResult.Value);
    }

    [Fact]
    public void ToCreatedAtActionResult_Success_ReturnsCreatedValue()
    {
        var result = Result.Success<string, TestError>("value");

        var actionResult = result.ToCreatedAtActionResult(
            "Get",
            new { id = 42 });

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal("Get", createdResult.ActionName);
        Assert.Equal(42, createdResult.RouteValues!["id"]);
        Assert.Equal("value", createdResult.Value);
    }

    [Fact]
    public void ToActionResult_CustomSuccess_ReturnsCallerSelectedResult()
    {
        var result = Result.Success<string, TestError>("value");

        var actionResult = result.ToActionResult(
            value => new AcceptedResult(location: null, value: value));

        var acceptedResult = Assert.IsType<AcceptedResult>(actionResult.Result);
        Assert.Equal("value", acceptedResult.Value);
    }

    [Fact]
    public void ToNoContentActionResult_Success_ReturnsNoContent()
    {
        var result = UnitResult.Success<TestError>();

        var actionResult = result.ToNoContentActionResult();

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToNoContentActionResult_Failure_ReturnsProblemDetails()
    {
        var error = new TestError(
            new ErrorDefinition("test.conflict", "Conflict.", ErrorKind.Conflict));
        var result = UnitResult.Failure(error);

        var actionResult = result.ToNoContentActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
    }

    [Fact]
    public void ToActionResult_NullSuccessDelegate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => Result.Success<string, TestError>("value")
                .ToActionResult(null!));
        Assert.Throws<ArgumentNullException>(
            () => UnitResult.Success<TestError>()
                .ToActionResult(null!));
    }

    [Fact]
    public void ToActionResult_UninitializedResults_ThrowInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => default(Result<string, TestError>).ToOkActionResult());
        Assert.Throws<InvalidOperationException>(
            () => default(UnitResult<TestError>).ToNoContentActionResult());
    }

    [Fact]
    public void ToActionResult_NullDefinition_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<string, NullDefinitionError>(new());

        Assert.Throws<InvalidOperationException>(() => result.ToOkActionResult());
    }

    [Fact]
    public async Task ToOkActionResult_FailureExecution_AppliesSharedProblemDetailsPolicy()
    {
        var error = new TestError(
            new ErrorDefinition("test.not_found", "Not found.", ErrorKind.NotFound));
        var result = Result.Failure<string, TestError>(error);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddControllers();
        serviceCollection.AddProblemDetails(
            options => options.CustomizeProblemDetails = problemContext =>
            {
                problemContext.ProblemDetails.Extensions["customized"] = true;
                problemContext.ProblemDetails.Extensions["hasException"] =
                    problemContext.Exception is not null;
            });
        var services = serviceCollection.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Path = "/test";
        context.TraceIdentifier = "trace-123";
        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor());

        var actionResult = result.ToOkActionResult();
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        await objectResult.ExecuteResultAsync(actionContext);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var response = document.RootElement;
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("/test", problemDetails.Instance);
        Assert.Equal(
            Activity.Current?.Id ?? "trace-123",
            response.GetProperty("traceId").GetString());
        Assert.True(response.GetProperty("customized").GetBoolean());
        Assert.False(response.GetProperty("hasException").GetBoolean());
        Assert.Equal(MediaTypeNames.Application.ProblemJson, context.Response.ContentType);
    }

    [Fact]
    public async Task ToOkActionResult_UnsupportedAccept_SerializesSelectedProblemDetails()
    {
        var error = new TestError(
            new ErrorDefinition("test.not_found", "Not found.", ErrorKind.NotFound));
        var result = Result.Failure<string, TestError>(error);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddControllers();
        serviceCollection.AddProblemDetails();
        var services = serviceCollection.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Headers.Accept = MediaTypeNames.Application.Xml;
        context.Request.Path = "/test";
        context.TraceIdentifier = "trace-123";
        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor());

        var actionResult = result.ToOkActionResult();
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        await objectResult.ExecuteResultAsync(actionContext);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var response = document.RootElement;
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.ProblemJson, context.Response.ContentType);
        Assert.Equal("Not found.", response.GetProperty("detail").GetString());
        Assert.Equal("test.not_found", response.GetProperty("code").GetString());
        Assert.Equal("/test", response.GetProperty("instance").GetString());
        Assert.Equal(
            Activity.Current?.Id ?? "trace-123",
            response.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task ToOkActionResult_ValidationFallbackWriter_SerializesErrors()
    {
        var error = new TestError(
            ErrorDefinition.Validation(
                "ticket.purchase_invalid",
                "The ticket purchase is invalid.",
                new Dictionary<string, string[]>
                {
                    ["quantity"] = ["Quantity must be positive."]
                }));
        var result = Result.Failure<string, TestError>(error);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddControllers();
        serviceCollection.AddProblemDetails();
        var services = serviceCollection.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Headers.Accept = MediaTypeNames.Application.Xml;
        context.Request.Path = "/test";
        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor());

        var actionResult = result.ToOkActionResult();
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        await objectResult.ExecuteResultAsync(actionContext);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var response = document.RootElement;
        Assert.Single(response.EnumerateObject().Where(property => property.NameEquals("errors")));
        Assert.Equal(
            "Quantity must be positive.",
            response.GetProperty("errors").GetProperty("quantity")[0].GetString());
        Assert.Equal("ticket.purchase_invalid", response.GetProperty("code").GetString());
        Assert.Equal(MediaTypeNames.Application.ProblemJson, context.Response.ContentType);
    }

    private sealed record TestError(ErrorDefinition Definition) : IError;

    private sealed class NullDefinitionError : IError
    {
        public ErrorDefinition Definition => null!;
    }
}
