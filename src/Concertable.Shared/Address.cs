namespace Concertable.Shared;

public record Address
{
    public string County { get; init; }
    public string Town { get; init; }

    public Address(string county, string town)
    {
        if (string.IsNullOrWhiteSpace(county))
            throw new DomainException("County cannot be empty.");
        if (string.IsNullOrWhiteSpace(town))
            throw new DomainException("Town cannot be empty.");

        County = county;
        Town = town;
    }
}
