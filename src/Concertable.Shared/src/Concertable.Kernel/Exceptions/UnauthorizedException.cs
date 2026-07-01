using System.Net;

namespace Concertable.Kernel.Exceptions;

public sealed class UnauthorizedException : HttpException
{
    public UnauthorizedException(string detail) : base(detail, HttpStatusCode.Unauthorized)
    {
        Title = "Unauthorized";
    }
}
