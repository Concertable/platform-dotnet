using Concertable.Kernel.ValueObjects;

namespace Concertable.Shared.Geocoding.Application;

public interface IGeocodingClient
{
    Task<Address> GetLocationAsync(double latitude, double longitude);
}
