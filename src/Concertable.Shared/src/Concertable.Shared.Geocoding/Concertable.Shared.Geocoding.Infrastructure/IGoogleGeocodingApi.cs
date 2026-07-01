using Refit;

namespace Concertable.Shared.Geocoding.Infrastructure;

internal interface IGoogleGeocodingApi
{
    [Get("/json")]
    Task<GoogleGeocodingResponse> GetAsync([Query] string latlng);
}
