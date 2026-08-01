using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;
using Concertable.Shared.Api.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Shared.Api.UnitTests;

public sealed class TypedErrorCompositionTests
{
    [Fact]
    public void Definition_ConcertNotFound_ReturnsStableNotFoundError()
    {
        PurchaseError error = PurchaseError.NotFound(42);

        var definition = error.Definition;

        Assert.Equal("ticket.concert_not_found", definition.Code);
        Assert.Equal("Concert not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
        Assert.IsNotType<ValidationErrorDefinition>(definition);
    }

    [Fact]
    public void Definition_Validation_ReturnsStructuredValidationError()
    {
        IReadOnlyDictionary<string, string[]> errors =
            new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be positive."]
            };
        PurchaseError error = PurchaseError.Invalid(errors);

        var definition = Assert.IsType<ValidationErrorDefinition>(error.Definition);

        Assert.Equal("ticket.purchase_invalid", definition.Code);
        Assert.Equal("The ticket purchase is invalid.", definition.Message);
        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Same(errors, definition.Errors);
    }

    [Fact]
    public void MapError_DependencyFailure_ProducesOwningTypedError()
    {
        var dependencyResult = Result.Failure<string, PaymentFailure>(
            new PaymentFailure("payment.card_declined", "The card was declined."));

        var result = dependencyResult.MapError(
            failure => PurchaseError.Rejected(failure.Code, failure.Message));

        Assert.True(result.TryGetError(out var error));
        var definition = error.Definition;
        Assert.Equal("payment.card_declined", definition.Code);
        Assert.Equal("The card was declined.", definition.Message);
        Assert.Equal(ErrorKind.PaymentRequired, definition.Kind);
    }

    [Fact]
    public void ToOkActionResult_TypedFailure_ReachesHttpTerminal()
    {
        var result = Result.Failure<string, PurchaseError>(
            PurchaseError.NotFound(42));

        var actionResult = result.ToOkActionResult();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("ticket.concert_not_found", problemDetails.Extensions["code"]);
    }

    private sealed record PaymentFailure(string Code, string Message);
}

internal sealed record PurchaseError(ErrorDefinition Definition) : IError
{
    public static PurchaseError NotFound(int concertId) =>
        new(ErrorDefinition.NotFound(
            "ticket.concert_not_found",
            "Concert not found."));

    public static PurchaseError Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorDefinition.Validation(
            "ticket.purchase_invalid",
            "The ticket purchase is invalid.",
            errors));

    public static PurchaseError Rejected(string code, string message) =>
        new(ErrorDefinition.PaymentRequired(code, message));
}
