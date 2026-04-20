using NetTopologySuite.Geometries;

namespace Concertable.Application.Mappers;

public static class PointMappers
{
    public static double? ToLatitude(this Point? point) => point?.Y;

    public static double? ToLongitude(this Point? point) => point?.X;
}
