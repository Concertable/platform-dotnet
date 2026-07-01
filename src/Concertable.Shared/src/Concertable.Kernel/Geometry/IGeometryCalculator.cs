namespace Concertable.Kernel.Geometry;

public interface IGeometryCalculator
{
    bool IsWithinRadius(double lat1, double lon1, double lat2, double lon2, int radiusKm);
}
