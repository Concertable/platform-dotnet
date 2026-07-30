using Concertable.Kernel.ValueObjects;

namespace Concertable.Kernel.UnitTests.ValueObjects;

public sealed class MoneyExtensionsTests
{
    #region ToMoney

    [Theory]
    [InlineData(1234, 12.34)]
    [InlineData(0, 0)]
    [InlineData(1, 0.01)]
    [InlineData(-1235, -12.35)]
    [InlineData(100000, 1000)]
    public void ToMoney_ConvertsMinorUnitsToGbpAmount(long minor, decimal expected)
    {
        var money = minor.ToMoney(Currency.Gbp);

        Assert.Equal(expected, money.Amount);
        Assert.Equal(Currency.Gbp, money.Currency);
    }

    [Fact]
    public void ToMoney_MatchesMoneyFromMinorUnits()
    {
        Assert.Equal(Money.FromMinorUnits(1234, Currency.Gbp), 1234L.ToMoney(Currency.Gbp));
    }

    [Theory]
    [InlineData(1234)]
    [InlineData(0)]
    [InlineData(-9999)]
    public void ToMoney_ThenToMinorUnits_RoundTrips(long minor)
    {
        Assert.Equal(minor, minor.ToMoney(Currency.Gbp).ToMinorUnits());
    }

    #endregion

    #region ToGbp

    [Theory]
    [InlineData(1234, 12.34)]
    [InlineData(0, 0)]
    [InlineData(1, 0.01)]
    [InlineData(-1235, -12.35)]
    [InlineData(100000, 1000)]
    public void ToGbp_ConvertsMinorUnitsToGbpAmount(long minor, decimal expected)
    {
        var money = minor.ToGbp();

        Assert.Equal(expected, money.Amount);
        Assert.Equal(Currency.Gbp, money.Currency);
    }

    [Fact]
    public void ToGbp_MatchesToMoneyWithGbp()
    {
        Assert.Equal(1234L.ToMoney(Currency.Gbp), 1234L.ToGbp());
    }

    [Theory]
    [InlineData(1234)]
    [InlineData(0)]
    [InlineData(-9999)]
    public void ToGbp_ThenToMinorUnits_RoundTrips(long minor)
    {
        Assert.Equal(minor, minor.ToGbp().ToMinorUnits());
    }

    #endregion
}
