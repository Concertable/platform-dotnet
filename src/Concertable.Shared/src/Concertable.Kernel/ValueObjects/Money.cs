namespace Concertable.Kernel.ValueObjects;

public readonly record struct Money(decimal Amount, Currency Currency)
{
    public long ToMinorUnits() => (long)Math.Round(Amount * 100m, 0, MidpointRounding.AwayFromZero);

    public static Money FromMinorUnits(long minor, Currency currency) => new(minor / 100m, currency);

    public static Money Zero(Currency currency) => new(0m, currency);

    public static Money Gbp(decimal amount) => new(amount, Currency.Gbp);

    public static Money operator +(Money a, Money b) => SameCurrency(a, b) with { Amount = a.Amount + b.Amount };

    public static Money operator -(Money a, Money b) => SameCurrency(a, b) with { Amount = a.Amount - b.Amount };

    private static Money SameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException($"Cannot operate on {a.Currency} and {b.Currency} amounts.");

        return a;
    }
}
