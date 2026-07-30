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
    public void Descriptor_ConcertNotFound_ReturnsStableNotFoundError()
    {
        PurchaseError error = new PurchaseError.ConcertNotFound(42);

        var descriptor = error.Descriptor;

        Assert.Equal("ticket.concert_not_found", descriptor.Code);
        Assert.Equal("Concert 42 was not found.", descriptor.Message);
        Assert.Equal(ErrorKind.NotFound, descriptor.Kind);
        Assert.IsNotType<ValidationErrorDescriptor>(descriptor);
    }

    [Fact]
    public void Descriptor_Validation_ReturnsStructuredValidationError()
    {
        IReadOnlyDictionary<string, string[]> errors =
            new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be positive."]
            };
        PurchaseError error = new PurchaseError.Validation(errors);

        var descriptor = Assert.IsType<ValidationErrorDescriptor>(error.Descriptor);

        Assert.Equal("ticket.purchase_invalid", descriptor.Code);
        Assert.Equal("The ticket purchase is invalid.", descriptor.Message);
        Assert.Equal(ErrorKind.Invalid, descriptor.Kind);
        Assert.Same(errors, descriptor.Errors);
    }

    [Fact]
    public void MapError_DependencyFailure_ProducesOwningUnionCase()
    {
        var dependencyResult = Result.Failure<string, PaymentFailure>(
            new PaymentFailure("payment.card_declined", "The card was declined."));

        var result = dependencyResult.MapError(
            failure => (PurchaseError)new PurchaseError.PaymentRejected(
                failure.Code,
                failure.Message));

        var descriptor = result.Error.Descriptor;
        Assert.Equal("payment.card_declined", descriptor.Code);
        Assert.Equal("The card was declined.", descriptor.Message);
        Assert.Equal(ErrorKind.PaymentRequired, descriptor.Kind);
    }

    [Fact]
    public void ToOkActionResult_UnionFailure_ReachesHttpTerminal()
    {
        var result = Result.Failure<string, PurchaseError>(
            new PurchaseError.ConcertNotFound(42));

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

    public ErrorDescriptor Descriptor => Match<ErrorDescriptor>(
        notFound => new ErrorDescriptor(
            "ticket.concert_not_found",
            $"Concert {notFound.ConcertId} was not found.",
            ErrorKind.NotFound),
        validation => new ValidationErrorDescriptor(
            "ticket.purchase_invalid",
            "The ticket purchase is invalid.",
            validation.Errors),
        paymentRejected => new ErrorDescriptor(
            paymentRejected.Code,
            paymentRejected.Message,
            ErrorKind.PaymentRequired));
}
