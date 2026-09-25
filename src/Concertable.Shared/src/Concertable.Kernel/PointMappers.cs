using NetTopologySuite.Geometries;

namespace Concertable.Kernel;

public static class PointMappers
{
    extension(Point? point)
    {
        public double? ToLatitude() => point?.Y;

        public double? ToLongitude() => point?.X;
    }
}
