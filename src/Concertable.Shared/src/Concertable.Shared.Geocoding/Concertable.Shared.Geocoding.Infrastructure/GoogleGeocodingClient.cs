using System.Globalization;
using Concertable.Kernel;
using Concertable.Kernel.Exceptions;
using Concertable.Shared.Geocoding.Application;

namespace Concertable.Shared.Geocoding.Infrastructure;

internal sealed class GoogleGeocodingClient : IGeocodingClient
{
    private readonly IGoogleGeocodingApi api;

    public GoogleGeocodingClient(IGoogleGeocodingApi api)
    {
        this.api = api;
    }

    public async Task<Address> GetLocationAsync(double latitude, double longitude)
    {
        var latLng = string.Create(CultureInfo.InvariantCulture, $"{latitude},{longitude}");
        var response = await api.GetAsync(latLng);

        if (response.Results.Count == 0)
            throw new BadRequestException("No geocoding results found for the provided coordinates.");

        string? county = null;
        string? town = null;

        foreach (var address in response.Results)
        {
            foreach (var component in address.AddressComponents)
            {
                if (component.Types.Contains("administrative_area_level_2"))
                    county ??= component.LongName;
                else if (component.Types.Contains("postal_town"))
                    town ??= component.LongName;
            }

            if (county is not null && town is not null)
                break;
        }

        if (county is null || town is null)
            throw new BadRequestException("County or Town not found");

        return new Address(county, town);
    }
}
