using Concertable.Kernel.ValueObjects;
using Concertable.Shared.Geocoding.Application;

namespace Concertable.Testing.Integration.Mocks;

public sealed class MockGeocodingClient : IGeocodingClient
{
    public Task<Address> GetLocationAsync(double latitude, double longitude)
        => Task.FromResult(new Address("Test County", "Test Town"));
}
