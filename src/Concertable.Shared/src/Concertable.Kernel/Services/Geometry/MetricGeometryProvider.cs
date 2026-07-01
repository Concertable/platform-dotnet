using Concertable.Kernel.Geometry;
using NetTopologySuite.Geometries;

namespace Concertable.Kernel.Services.Geometry;

public sealed class MetricGeometryProvider : IGeometryProvider
{
    private readonly GeometryFactory geometryFactory;

    public MetricGeometryProvider(GeometryFactory geometryFactory)
    {
        this.geometryFactory = geometryFactory;
    }

    public Point CreatePoint(double latitude, double longitude)
    {
        var x = longitude * 20037508.34 / 180;
        var y = Math.Log(Math.Tan((90 + latitude) * Math.PI / 360)) / (Math.PI / 180) * 20037508.34 / 180;
        return geometryFactory.CreatePoint(new Coordinate(x, y));
    }

    public Point? CreatePoint(double? latitude, double? longitude)
    {
        return (latitude.HasValue && longitude.HasValue)
            ? CreatePoint(latitude.Value, longitude.Value)
            : null;
    }
}
