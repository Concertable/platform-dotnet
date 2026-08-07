using Grpc.Core;

namespace Concertable.Grpc;

public static class RpcExceptionExtensions
{
    public static bool IsClientCancellation(
        this RpcException exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.StatusCode == StatusCode.Cancelled
            && cancellationToken.IsCancellationRequested;
    }
}
