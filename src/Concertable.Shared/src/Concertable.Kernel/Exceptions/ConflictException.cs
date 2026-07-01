using System.Net;

namespace Concertable.Kernel.Exceptions;

public sealed class ConflictException : HttpException
{
    public ConflictException(string detail)
        : base(detail, HttpStatusCode.Conflict)
    {
        Title = "Conflict";
    }
}
