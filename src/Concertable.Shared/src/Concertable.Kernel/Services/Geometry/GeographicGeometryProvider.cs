using Concertable.Kernel.Geometry;
using NetTopologySuite.Geometries;

namespace Concertable.Kernel.Services.Geometry;

public sealed class GeographicGeometryProvider : IGeometryProvider
{
    private readonly GeometryFactory geometryFactory;

    public GeographicGeometryProvider(GeometryFactory geometryFactory)
    {
        this.geometryFactory = geometryFactory;
    }

    public Point CreatePoint(double latitude, double longitude)
    {
        return geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
    }

    public Point? CreatePoint(double? latitude, double? longitude)
    {
        return (latitude.HasValue && longitude.HasValue)
            ? CreatePoint(latitude.Value, longitude.Value)
            : null;
    }
}
