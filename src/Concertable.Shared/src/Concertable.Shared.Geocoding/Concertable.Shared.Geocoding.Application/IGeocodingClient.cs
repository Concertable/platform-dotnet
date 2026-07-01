using Concertable.Kernel;

namespace Concertable.Shared.Geocoding.Application;

public interface IGeocodingClient
{
    Task<Address> GetLocationAsync(double latitude, double longitude);
}
