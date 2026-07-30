using Concertable.Kernel.Errors;

namespace Concertable.Kernel.UnitTests;

public sealed class ErrorDescriptorTests
{
    [Theory]
    [InlineData("ticket.concert_not_found")]
    [InlineData("payment.card_declined")]
    [InlineData("dependency.timeout")]
    public void Constructor_ValidCode_PreservesDescriptor(string code)
    {
        var descriptor = new ErrorDescriptor(code, "Safe message.", ErrorKind.Conflict);

        Assert.Equal(code, descriptor.Code);
        Assert.Equal("Safe message.", descriptor.Message);
        Assert.Equal(ErrorKind.Conflict, descriptor.Kind);
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
            () => new ErrorDescriptor(code!, "Safe message.", ErrorKind.Invalid));

        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingMessage_ThrowsArgumentException(string? message)
    {
        var exception = Record.Exception(
            () => new ErrorDescriptor("ticket.invalid", message!, ErrorKind.Invalid));

        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Fact]
    public void Constructor_UnknownKind_ThrowsArgumentOutOfRangeException()
    {
        var exception = Record.Exception(
            () => new ErrorDescriptor(
                "ticket.invalid",
                "Safe message.",
                (ErrorKind)int.MaxValue));

        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void With_InvalidCode_ThrowsArgumentException()
    {
        var descriptor = new ErrorDescriptor(
            "ticket.invalid",
            "Safe message.",
            ErrorKind.Invalid);

        var exception = Record.Exception(() => descriptor with { Code = "Invalid" });

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void ValidationConstructor_NoErrors_ThrowsArgumentException()
    {
        var exception = Record.Exception(
            () => new ValidationErrorDescriptor(
                "ticket.invalid",
                "Safe message.",
                new Dictionary<string, string[]>()));

        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void ValidationConstructor_BlankErrorMessage_ThrowsArgumentException()
    {
        var exception = Record.Exception(
            () => new ValidationErrorDescriptor(
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
        var descriptor = new ValidationErrorDescriptor(
            "ticket.invalid",
            "Safe message.",
            new Dictionary<string, string[]>
            {
                ["quantity"] = ["Quantity must be positive."]
            });

        var exception = Record.Exception(
            () => descriptor with
            {
                Errors = new Dictionary<string, string[]>()
            });

        Assert.IsType<ArgumentException>(exception);
    }
}
