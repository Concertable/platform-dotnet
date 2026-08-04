using Concertable.Kernel.Errors;

namespace Concertable.Kernel.UnitTests;

public sealed class ErrorCodeResolverTests
{
    [Theory]
    [MemberData(nameof(DerivedCodes))]
    public void Of_UnionAndCaseNames_DerivesExpectedCode(Type caseType, string expectedCode)
    {
        Assert.Equal(expectedCode, ErrorCodeResolver.Of(caseType));
    }

    [Fact]
    public void Of_ErrorCodeAttribute_PreservesPublishedCode()
    {
        Assert.Equal(
            "escrow.refund_not_allowed",
            ErrorCodeResolver.Of<EscrowRefundError.EscrowRejected>());
    }

    [Fact]
    public void Of_ErrorCodeAttributeOnUnion_IsNotInheritedByCase()
    {
        Assert.Equal(
            "inheritance.mandate_not_found",
            ErrorCodeResolver.Of<InheritanceError.MandateNotFound>());
    }

    [Fact]
    public void Of_RepeatedResolution_ReturnsCachedCode()
    {
        var first = ErrorCodeResolver.Of<PaymentError.AlreadyCaptured>();
        var second = ErrorCodeResolver.Of<PaymentError.AlreadyCaptured>();

        Assert.Same(first, second);
    }

    [Theory]
    [MemberData(nameof(UnderivableCases))]
    public void Of_UnderivableCase_ThrowsInvalidOperationException(Type caseType)
    {
        Assert.Throws<InvalidOperationException>(() => ErrorCodeResolver.Of(caseType));
    }

    public static TheoryData<Type, string> DerivedCodes => new()
    {
        { typeof(PaymentError.InvalidRequest), "payment.invalid_request" },
        { typeof(PaymentError.PayerNotFound), "payment.payer_not_found" },
        { typeof(PaymentError.DeclinedCase), "payment.declined" },
        { typeof(PaymentError.NotFound), "payment.not_found" },
        { typeof(CommissionError.BindingNotFound), "commission.binding_not_found" },
        { typeof(CommissionError.RateNotFoundCase), "commission.rate_not_found" },
        { typeof(EscrowRefundError.EscrowNotFound), "escrow.refund_not_found" },
        { typeof(EscrowRefundError.RefundNotFound), "escrow.refund_not_found" },
        { typeof(EscrowRefundError.CurrencyMismatch), "escrow.refund_currency_mismatch" },
        { typeof(GatewayError.HTTP2Unavailable), "gateway.http_2_unavailable" },
        { typeof(GatewayError.ACHMandateNotFound), "gateway.ach_mandate_not_found" }
    };

    public static TheoryData<Type> UnderivableCases => new()
    {
        typeof(UnnestedNotFound),
        typeof(UnsuffixedUnion.NotFound),
        typeof(Error.NotFound),
        typeof(SingleWordError.Case),
        typeof(EscrowError.Escrow),
        typeof(PaymentError.Legacy_NotFound)
    };
}
