namespace Concertable.Kernel.ValueObjects;

public sealed record Address
{
    public string County { get; init; }
    public string Town { get; init; }

    public Address(string county, string town)
    {
        DomainException.ThrowIfNullOrWhiteSpace(county, "County");
        DomainException.ThrowIfNullOrWhiteSpace(town, "Town");

        County = county;
        Town = town;
    }
}
