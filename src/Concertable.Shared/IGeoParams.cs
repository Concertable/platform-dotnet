namespace Concertable.Shared;

public interface IGeoParams
{
    double? Latitude { get; }
    double? Longitude { get; }
    int? RadiusKm { get; }
}
