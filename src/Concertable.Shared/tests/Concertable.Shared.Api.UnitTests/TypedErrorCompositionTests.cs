using System.ComponentModel;
using Concertable.Kernel.Errors;
using Concertable.Shared.Api.Results;
using CSharpFunctionalExtensions;
using Dunet;
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
        Assert.Equal(ErrorKind.NotFound, error.Kind);
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
        Assert.Equal(ErrorKind.Invalid, error.Kind);
        Assert.Same(errors, definition.Errors);
    }

    [Fact]
    public void MapError_DependencyFailure_ProducesOwningUnionCase()
    {
        var dependencyResult = Result.Failure<string, PaymentFailure>(
            new PaymentFailure("payment.card_declined", "The card was declined."));

        var result = dependencyResult.MapError(
            failure => PurchaseError.Rejected(failure.Code, failure.Message));

        var definition = result.Error.Definition;
        Assert.Equal("payment.card_declined", definition.Code);
        Assert.Equal("The card was declined.", definition.Message);
        Assert.Equal(ErrorKind.PaymentRequired, result.Error.Kind);
    }

    [Fact]
    public void ToOkActionResult_UnionFailure_ReachesHttpTerminal()
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

[Union]
internal partial record PurchaseError : IError
{
    partial record ConcertNotFound(int ConcertId);
    partial record Validation(IReadOnlyDictionary<string, string[]> Errors);
    partial record PaymentRejected(string Code, string Message);

    public static PurchaseError NotFound(int concertId) =>
        new ConcertNotFound(concertId);

    public static PurchaseError Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new Validation(errors);

    public static PurchaseError Rejected(string code, string message) =>
        new PaymentRejected(code, message);

    public ErrorDefinition Definition => Match<ErrorDefinition>(
        notFound => ErrorDefinition.NotFound<ConcertResource>(
            "ticket.concert_not_found"),
        validation => ErrorDefinition.Validation(
            "ticket.purchase_invalid",
            "The ticket purchase is invalid.",
            validation.Errors),
        paymentRejected => ErrorDefinition.PaymentRequired(
            paymentRejected.Code,
            paymentRejected.Message));

    public ErrorKind Kind => Definition.Kind;
}

[DisplayName("Concert")]
internal sealed class ConcertResource;
