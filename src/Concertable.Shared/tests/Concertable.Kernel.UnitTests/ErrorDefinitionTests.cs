using System.ComponentModel;
using Concertable.Kernel.Errors;

namespace Concertable.Kernel.UnitTests;

public sealed class ErrorDefinitionTests
{
    [Fact]
    public void Factories_SemanticKinds_ReturnMatchingDefinitions()
    {
        var definitions = new[]
        {
            ErrorDefinition.Invalid("test.invalid", "Invalid."),
            ErrorDefinition.NotFound("test.not_found", "Not found."),
            ErrorDefinition.Conflict("test.conflict", "Conflict."),
            ErrorDefinition.Unauthenticated("test.unauthenticated", "Unauthenticated."),
            ErrorDefinition.Forbidden("test.forbidden", "Forbidden."),
            ErrorDefinition.PaymentRequired("test.payment_required", "Payment required.")
        };

        Assert.Equal(Enum.GetValues<ErrorKind>(), definitions.Select(definition => definition.Kind));
    }

    [Fact]
    public void ValidationFactory_Errors_ReturnsValidationDefinition()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["quantity"] = ["Quantity must be positive."]
        };

        var definition = ErrorDefinition.Validation(
            "test.invalid",
            "Invalid.",
            errors);

        Assert.Equal(ErrorKind.Invalid, definition.Kind);
        Assert.Same(errors, definition.Errors);
    }

    [Fact]
    public void NotFoundFactory_AnnotatedType_DerivesSafeMessage()
    {
        var definition = ErrorDefinition.NotFound<Widget>("test.not_found");

        Assert.Equal("Widget not found.", definition.Message);
        Assert.Equal(ErrorKind.NotFound, definition.Kind);
    }

    [Fact]
    public void CaseFactories_EveryKind_DeriveCodeMessageAndKind()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["amount"] = ["Amount must be positive."]
        };

        var definitions = new ErrorDefinition[]
        {
            ErrorDefinition.Invalid<PaymentError.InvalidRequest>(),
            ErrorDefinition.NotFound<PaymentError.PayerNotFound>(),
            ErrorDefinition.Conflict<PaymentError.AlreadyCaptured>(),
            ErrorDefinition.Unauthenticated<PaymentError.AuthenticationRequired>(),
            ErrorDefinition.Forbidden<PaymentError.AccessForbidden>(),
            ErrorDefinition.PaymentRequired<PaymentError.DeclinedCase>(),
            ErrorDefinition.Validation<PaymentError.ValidationFailed>(errors)
        };

        Assert.Equal(
            [
                "payment.invalid_request",
                "payment.payer_not_found",
                "payment.already_captured",
                "payment.authentication_required",
                "payment.access_forbidden",
                "payment.declined",
                "payment.validation_failed"
            ],
            definitions.Select(definition => definition.Code));
        Assert.Equal(
            [
                "Invalid request.",
                "Payer not found.",
                "Already captured.",
                "Authentication required.",
                "Access forbidden.",
                "Declined.",
                "Validation failed."
            ],
            definitions.Select(definition => definition.Message));
        Assert.Equal(
            [
                ErrorKind.Invalid,
                ErrorKind.NotFound,
                ErrorKind.Conflict,
                ErrorKind.Unauthenticated,
                ErrorKind.Forbidden,
                ErrorKind.PaymentRequired,
                ErrorKind.Invalid
            ],
            definitions.Select(definition => definition.Kind));
        Assert.Same(errors, Assert.IsType<ValidationErrorDefinition>(definitions[^1]).Errors);
    }

    [Fact]
    public void NotFoundCaseFactory_HumanizesCaseName()
    {
        var definition = ErrorDefinition.NotFound<PaymentError.PayerNotFound>();

        Assert.Equal("Payer not found.", definition.Message);
    }

    [Fact]
    public void NotFoundCaseFactory_OtherUnion_HumanizesCaseName()
    {
        var definition = ErrorDefinition.NotFound<CommissionError.BindingNotFound>();

        Assert.Equal("Binding not found.", definition.Message);
    }

    [Fact]
    public void CaseFactory_AcronymAndNumber_HumanizesCaseName()
    {
        var definition = ErrorDefinition.Invalid<GatewayError.HTTP2Unavailable>();

        Assert.Equal("HTTP 2 unavailable.", definition.Message);
    }

    [Fact]
    public void CaseFactory_ErrorCodeAttribute_PreservesPublishedCode()
    {
        var definition = ErrorDefinition.Conflict<EscrowRefundError.EscrowRejected>(
            "The escrow cannot be refunded in its current state.");

        Assert.Equal("escrow.refund_not_allowed", definition.Code);
    }

    [Fact]
    public void CaseFactory_MalformedErrorCodeAttribute_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => ErrorDefinition.Conflict<EscrowRefundError.MalformedOverride>("Conflict."));
    }

    [Theory]
    [InlineData("ticket.concert_not_found")]
    [InlineData("payment.card_declined")]
    [InlineData("dependency.timeout")]
    public void Constructor_ValidCode_PreservesDefinition(string code)
    {
        var definition = new ErrorDefinition(code, "Safe message.", ErrorKind.Conflict);

        Assert.Equal(code, definition.Code);
        Assert.Equal("Safe message.", definition.Message);
        Assert.Equal(ErrorKind.Conflict, definition.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("ticket")]
    [InlineData("Ticket.not_found")]
    [InlineData("ticket.not-found")]
    [InlineData(".ticket")]
    public void Constructor_InvalidCode_ThrowsArgumentException(string? code)
    {
        var exception = Record.Exception(
            () => new ErrorDefinition(code!, "Safe message.", ErrorKind.Invalid));

        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingMessage_ThrowsArgumentException(string? message)
    {
        var exception = Record.Exception(
            () => new ErrorDefinition("ticket.invalid", message!, ErrorKind.Invalid));

        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Fact]
    public void Constructor_UnknownKind_ThrowsArgumentOutOfRangeException()
    {
        var exception = Record.Exception(
            () => new ErrorDefinition(
                "ticket.invalid",
                "Safe message.",
                (ErrorKind)int.MaxValue));

        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void With_InvalidCode_ThrowsArgumentException()
    {
        var definition = new ErrorDefinition(
            "ticket.invalid",
            "Safe message.",
            ErrorKind.Invalid);

        var exception = Record.Exception(() => definition with { Code = "Invalid" });

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void ValidationConstructor_NoErrors_ThrowsArgumentException()
    {
        var exception = Record.Exception(
            () => new ValidationErrorDefinition(
                "ticket.invalid",
                "Safe message.",
                new Dictionary<string, string[]>()));

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void ValidationConstructor_BlankErrorMessage_ThrowsArgumentException()
    {
        var exception = Record.Exception(
            () => new ValidationErrorDefinition(
                "ticket.invalid",
                "Safe message.",
                new Dictionary<string, string[]>
                {
                    ["quantity"] = [string.Empty]
                }));

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void ValidationWith_NoErrors_ThrowsArgumentException()
    {
        var definition = new ValidationErrorDefinition(
            "ticket.invalid",
            "Safe message.",
            new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be positive."]
            });

        var exception = Record.Exception(
            () => definition with
            {
                Errors = new Dictionary<string, string[]>()
            });

        Assert.IsType<ArgumentException>(exception);
    }

    [DisplayName("Widget")]
    private sealed class Widget;
}
