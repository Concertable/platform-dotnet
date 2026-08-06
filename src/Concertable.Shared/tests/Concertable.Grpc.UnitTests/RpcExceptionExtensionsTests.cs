using Grpc.Core;

namespace Concertable.Grpc.UnitTests;

public sealed class RpcExceptionExtensionsTests
{
    [Fact]
    public void IsClientCancellation_CancelledStatusAndRequestedToken_ReturnsTrue()
    {
        var exception = CreateException(StatusCode.Cancelled);

        Assert.True(exception.IsClientCancellation(new CancellationToken(canceled: true)));
    }

    [Fact]
    public void IsClientCancellation_CancelledStatusWithoutRequestedToken_ReturnsFalse()
    {
        var exception = CreateException(StatusCode.Cancelled);

        Assert.False(exception.IsClientCancellation(CancellationToken.None));
    }

    [Fact]
    public void IsClientCancellation_OtherStatusAndRequestedToken_ReturnsFalse()
    {
        var exception = CreateException(StatusCode.Unavailable);

        Assert.False(exception.IsClientCancellation(new CancellationToken(canceled: true)));
    }

    private static RpcException CreateException(StatusCode statusCode) =>
        new(new Status(statusCode, string.Empty));
}
