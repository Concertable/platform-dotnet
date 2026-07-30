using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Results;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Mime;

namespace Concertable.Shared.Api.UnitTests;

public sealed class ResultHttpExtensionsTests
{
    [Theory]
    [InlineData(ErrorKind.Invalid, HttpStatusCode.BadRequest)]
    [InlineData(ErrorKind.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorKind.Conflict, HttpStatusCode.Conflict)]
    [InlineData(ErrorKind.Unauthenticated, HttpStatusCode.Unauthorized)]
    [InlineData(ErrorKind.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(ErrorKind.PaymentRequired, HttpStatusCode.PaymentRequired)]
    public void ToOkActionResult_FailedResult_MapsSemanticKind(
        ErrorKind kind,
        HttpStatusCode expectedStatus)
    {
        var error = new TestError(new ErrorDescriptor("test.code", "Safe detail.", kind));
        var result = Result.Failure<string, TestError>(error);

        var actionResult = result.ToOkActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal((int)expectedStatus, objectResult.StatusCode);
        Assert.Equal((int)expectedStatus, problemDetails.Status);
        Assert.Equal(
            ReasonPhrases.GetReasonPhrase((int)expectedStatus),
            problemDetails.Title);
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
            new ValidationErrorDescriptor(
                "ticket.purchase_invalid",
                "The ticket purchase is invalid.",
                validationErrors));
        var result = Result.Failure<string, TestError>(error);

        var actionResult = result.ToOkActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Same(validationErrors, problemDetails.Extensions["errors"]);
    }

    [Fact]
    public void ToOkActionResult_AllErrorKinds_HaveHttpMappings()
    {
        foreach (var kind in Enum.GetValues<ErrorKind>())
        {
            var error = new TestError(new ErrorDescriptor("test.code", "Safe detail.", kind));
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
            new ErrorDescriptor("test.conflict", "Conflict.", ErrorKind.Conflict));
        var result = UnitResult.Failure(error);

        var actionResult = result.ToNoContentActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
    }

    [Fact]
    public async Task ToOkActionResult_FailureExecution_SetsInstanceAndProblemContentType()
    {
        var error = new TestError(
            new ErrorDescriptor("test.not_found", "Not found.", ErrorKind.NotFound));
        var result = Result.Failure<string, TestError>(error);
        var services = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .Services
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Path = "/test";
        var actionContext = new ActionContext(
            context,
            new RouteData(),
            new ActionDescriptor());

        var actionResult = result.ToOkActionResult();
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        await objectResult.ExecuteResultAsync(actionContext);

        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("/test", problemDetails.Instance);
        Assert.StartsWith(MediaTypeNames.Application.ProblemJson, context.Response.ContentType);
    }

    private sealed record TestError(ErrorDescriptor Descriptor) : IError;
}
