namespace Concertable.Kernel.ValueObjects;

public static class MoneyExtensions
{
    extension(long minorUnits)
    {
        public Money ToMoney(Currency currency) =>
            Money.FromMinorUnits(minorUnits, currency);

        public Money ToGbp() =>
            Money.FromMinorUnits(minorUnits, Currency.Gbp);
    }
}
