using System.Net;

namespace Concertable.Kernel.Exceptions;

public sealed class PaymentRequiredException : HttpException
{
    public PaymentRequiredException(string detail) : base(detail, HttpStatusCode.PaymentRequired)
    {
        Title = "Payment Required";
    }
}
