using Concertable.Kernel.Exceptions;
using Concertable.Kernel.ValueObjects;
using Concertable.Shared.Geocoding.Application;

namespace Concertable.Testing.Integration.Mocks;

public sealed class MockGeocodingClientFail : IGeocodingClient
{
    public Task<Address> GetLocationAsync(double latitude, double longitude)
        => throw new BadRequestException("County or Town not found");
}
