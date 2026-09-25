using Grpc.Core;

namespace Concertable.Grpc;

public static class RpcExceptionExtensions
{
    extension(RpcException exception)
    {
        public bool IsClientCancellation(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.StatusCode == StatusCode.Cancelled
                && cancellationToken.IsCancellationRequested;
        }
    }
}
