using Concertable.Kernel.Extensions;
using CSharpFunctionalExtensions;

namespace Concertable.Kernel.UnitTests;

public sealed class MaybeResultExtensionsTests
{
    [Fact]
    public void OrFailure_PresentValue_ReturnsSuccess()
    {
        var maybe = Maybe.From("value");

        var result = maybe.OrFailure("missing");

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void OrFailure_MissingValue_ReturnsSuppliedError()
    {
        var maybe = Maybe<string>.None;

        var result = maybe.OrFailure("missing");

        Assert.True(result.IsFailure);
        Assert.Equal("missing", result.Error);
    }

    [Fact]
    public void OrFailure_PresentValue_DoesNotInvokeErrorFactory()
    {
        var maybe = Maybe.From("value");
        var invoked = false;

        var result = maybe.OrFailure(() =>
        {
            invoked = true;
            return "missing";
        });

        Assert.True(result.IsSuccess);
        Assert.False(invoked);
    }

    [Fact]
    public void OrFailure_MissingValue_InvokesErrorFactory()
    {
        var maybe = Maybe<string>.None;
        var invoked = false;

        var result = maybe.OrFailure(() =>
        {
            invoked = true;
            return "missing";
        });

        Assert.True(result.IsFailure);
        Assert.True(invoked);
        Assert.Equal("missing", result.Error);
    }
}
