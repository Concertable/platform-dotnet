using Concertable.Kernel.Exceptions;

namespace Concertable.Kernel.UnitTests;

public sealed class DependencyExceptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingDependencyName_ThrowsArgumentException(string? dependencyName)
    {
        var exception = Record.Exception(
            () => new DependencyUnavailableException(dependencyName!));

        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Fact]
    public void UnavailableConstructor_DependencyFailure_PreservesContext()
    {
        var cause = new InvalidOperationException("Provider detail.");

        var exception = new DependencyUnavailableException("Payment", cause);

        Assert.Equal("Payment", exception.DependencyName);
        Assert.Same(cause, exception.InnerException);
    }

    [Fact]
    public void TimeoutConstructor_DependencyFailure_PreservesContext()
    {
        var cause = new TimeoutException("Provider detail.");

        var exception = new DependencyTimeoutException("Payment", cause);

        Assert.Equal("Payment", exception.DependencyName);
        Assert.Same(cause, exception.InnerException);
    }
}
