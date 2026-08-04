using System.ComponentModel;
using Concertable.Kernel.Errors;

namespace Concertable.Kernel.UnitTests;

internal sealed record UnnestedNotFound;

internal abstract record PaymentError
{
    internal sealed record InvalidRequest : PaymentError;

    [DisplayName("Payer payment account")]
    internal sealed record PayerNotFound : PaymentError;

    internal sealed record AlreadyCaptured : PaymentError;

    internal sealed record AuthenticationRequired : PaymentError;

    internal sealed record AccessForbidden : PaymentError;

    internal sealed record DeclinedCase : PaymentError;

    internal sealed record ValidationFailed : PaymentError;

    internal sealed record NotFound : PaymentError;

    internal sealed record Legacy_NotFound : PaymentError;
}

internal abstract record CommissionError
{
    internal sealed record BindingNotFound : CommissionError;

    internal sealed record RateNotFoundCase : CommissionError;
}

internal abstract record EscrowRefundError
{
    internal sealed record EscrowNotFound : EscrowRefundError;

    internal sealed record RefundNotFound : EscrowRefundError;

    internal sealed record CurrencyMismatch : EscrowRefundError;

    [ErrorCode("escrow.refund_not_allowed")]
    internal sealed record EscrowRejected : EscrowRefundError;

    [ErrorCode("Not A Code")]
    internal sealed record MalformedOverride : EscrowRefundError;
}

internal abstract record GatewayError
{
    internal sealed record HTTP2Unavailable : GatewayError;

    internal sealed record ACHMandateNotFound : GatewayError;
}

[ErrorCode("union.declared_on_the_root")]
internal abstract record InheritanceError
{
    internal sealed record MandateNotFound : InheritanceError;
}

internal abstract record EscrowError
{
    internal sealed record Escrow : EscrowError;
}

internal abstract record SingleWordError
{
    internal sealed record Case : SingleWordError;
}

internal abstract record Error
{
    internal sealed record NotFound : Error;
}

internal abstract record UnsuffixedUnion
{
    internal sealed record NotFound : UnsuffixedUnion;
}
