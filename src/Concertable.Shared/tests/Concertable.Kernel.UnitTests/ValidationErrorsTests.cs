using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

namespace Concertable.Kernel.UnitTests;

public sealed class ValidationErrorsTests
{
    [Fact]
    public void Constructor_RepeatedKeys_PreservesMessageOrder()
    {
        var errors = new ValidationErrors(
        [
            new("quantity", "Quantity is required."),
            new("ticketType", "Ticket type is invalid."),
            new("quantity", "Quantity must be positive.")
        ]);

        Assert.Equal(
            ["Quantity is required.", "Quantity must be positive."],
            errors.Errors["quantity"]);
        Assert.Equal(["Ticket type is invalid."], errors.Errors["ticketType"]);
    }

    [Fact]
    public void Constructor_InvalidCollectionsKeysMessagesAndArrays_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ValidationErrors((IEnumerable<KeyValuePair<string, string>>)null!));
        Assert.Throws<ArgumentException>(() => new ValidationErrors([]));
        Assert.Throws<ArgumentException>(
            () => new ValidationErrors([new("", "message")]));
        Assert.Throws<ArgumentException>(
            () => new ValidationErrors([new("field", " ")]));
        Assert.Throws<ArgumentNullException>(
            () => new ValidationErrors(
                new Dictionary<string, string[]> { ["field"] = null! }));
        Assert.Throws<ArgumentException>(
            () => new ValidationErrors(
                new Dictionary<string, string[]> { ["field"] = [] }));
    }

    [Fact]
    public void ConstructorAndToDictionary_DefensivelyCopyMutableArrays()
    {
        var messages = new[] { "Original." };
        var source = new Dictionary<string, string[]> { ["field"] = messages };
        var errors = new ValidationErrors(source);

        messages[0] = "Changed input.";
        var firstSnapshot = errors.ToDictionary();
        firstSnapshot["field"][0] = "Changed output.";
        var secondSnapshot = errors.ToDictionary();

        Assert.Equal("Original.", errors.Errors["field"][0]);
        Assert.Equal("Original.", secondSnapshot["field"][0]);
    }

    [Fact]
    public void EqualityAndHashing_CompareKeysAndOrderedMessages()
    {
        var first = new ValidationErrors(
        [
            new("quantity", "Required."),
            new("quantity", "Positive."),
            new("ticketType", "Invalid.")
        ]);
        var reorderedKeys = new ValidationErrors(
        [
            new("ticketType", "Invalid."),
            new("quantity", "Required."),
            new("quantity", "Positive.")
        ]);
        var reorderedMessages = new ValidationErrors(
        [
            new("quantity", "Positive."),
            new("quantity", "Required."),
            new("ticketType", "Invalid.")
        ]);

        Assert.Equal(first, reorderedKeys);
        Assert.Equal(first.GetHashCode(), reorderedKeys.GetHashCode());
        Assert.NotEqual(first, reorderedMessages);
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void MapError_ValidationErrors_ProducesOwningOperationError()
    {
        var validationErrors = new ValidationErrors(
            [new("quantity", "Quantity must be positive.")]);
        var validation = UnitResult.Failure(validationErrors);

        var result = validation.MapError(
            errors => new TestError(
                ErrorDefinition.Validation(
                    "ticket.purchase_invalid",
                    "The ticket purchase is invalid.",
                    errors.ToDictionary())));

        Assert.True(result.TryGetError(out var error));
        var definition = Assert.IsType<ValidationErrorDefinition>(error.Definition);
        Assert.Equal(
            ["Quantity must be positive."],
            definition.Errors["quantity"]);
        Assert.False(typeof(IError).IsAssignableFrom(typeof(ValidationErrors)));
    }

    private sealed record TestError(ErrorDefinition Definition) : IError;
}
