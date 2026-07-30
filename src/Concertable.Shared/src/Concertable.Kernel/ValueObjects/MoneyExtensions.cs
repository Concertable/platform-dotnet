namespace Concertable.Kernel.ValueObjects;

public static class MoneyExtensions
{
    public static Money ToMoney(this long minorUnits, Currency currency) =>
        Money.FromMinorUnits(minorUnits, currency);

    public static Money ToGbp(this long minorUnits) =>
        Money.FromMinorUnits(minorUnits, Currency.Gbp);
}
