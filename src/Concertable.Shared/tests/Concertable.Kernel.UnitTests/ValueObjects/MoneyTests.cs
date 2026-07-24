using Concertable.Kernel.ValueObjects;

namespace Concertable.Kernel.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    #region ToMinorUnits

    [Theory]
    [InlineData(12.34, 1234)]
    [InlineData(0, 0)]
    [InlineData(0.01, 1)]
    [InlineData(100, 10000)]
    [InlineData(12.345, 1235)]
    [InlineData(12.344, 1234)]
    [InlineData(-12.345, -1235)]
    public void ToMinorUnits_RoundsAwayFromZeroToTheMinorUnit(decimal amount, long expected)
    {
        Assert.Equal(expected, new Money(amount, Currency.Gbp).ToMinorUnits());
    }

    #endregion

    #region FromMinorUnits

    [Theory]
    [InlineData(1234, 12.34)]
    [InlineData(0, 0)]
    [InlineData(1, 0.01)]
    [InlineData(-1235, -12.35)]
    public void FromMinorUnits_ConvertsToAmount(long minor, decimal expected)
    {
        var money = Money.FromMinorUnits(minor, Currency.Gbp);

        Assert.Equal(expected, money.Amount);
        Assert.Equal(Currency.Gbp, money.Currency);
    }

    [Theory]
    [InlineData(1234)]
    [InlineData(0)]
    [InlineData(-9999)]
    public void FromMinorUnits_ThenToMinorUnits_RoundTrips(long minor)
    {
        Assert.Equal(minor, Money.FromMinorUnits(minor, Currency.Gbp).ToMinorUnits());
    }

    #endregion

    #region Zero

    [Fact]
    public void Zero_IsZeroAmountInTheGivenCurrency()
    {
        var zero = Money.Zero(Currency.Gbp);

        Assert.Equal(0m, zero.Amount);
        Assert.Equal(Currency.Gbp, zero.Currency);
    }

    #endregion

    #region Arithmetic

    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var sum = new Money(12.34m, Currency.Gbp) + new Money(0.66m, Currency.Gbp);

        Assert.Equal(new Money(13.00m, Currency.Gbp), sum);
    }

    [Fact]
    public void Subtract_SameCurrency_SubtractsAmounts()
    {
        var difference = new Money(13.00m, Currency.Gbp) - new Money(0.66m, Currency.Gbp);

        Assert.Equal(new Money(12.34m, Currency.Gbp), difference);
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsDomainException()
    {
        var gbp = new Money(12.34m, Currency.Gbp);
        var usd = new Money(12.34m, (Currency)840);

        Assert.Throws<DomainException>(() => gbp + usd);
    }

    [Fact]
    public void Subtract_DifferentCurrencies_ThrowsDomainException()
    {
        var gbp = new Money(12.34m, Currency.Gbp);
        var usd = new Money(12.34m, (Currency)840);

        Assert.Throws<DomainException>(() => gbp - usd);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(new Money(12.34m, Currency.Gbp), new Money(12.34m, Currency.Gbp));
    }

    #endregion
}
