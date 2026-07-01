using NetTopologySuite.Geometries;

namespace Concertable.Kernel.Geometry;

public interface IGeometryProvider
{
    Point? CreatePoint(double? latitude, double? longitude);
    Point CreatePoint(double latitude, double longitude);
}
