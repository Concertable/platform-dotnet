namespace Concertable.Shared.Geocoding.Infrastructure;

internal sealed record GoogleGeocodingResponse
{
    public List<GoogleAddress> Results { get; init; } = [];
}

internal sealed record GoogleAddress
{
    public List<GoogleAddressComponent> AddressComponents { get; init; } = [];
}

internal sealed record GoogleAddressComponent
{
    public string LongName { get; init; } = null!;
    public List<string> Types { get; init; } = [];
}
