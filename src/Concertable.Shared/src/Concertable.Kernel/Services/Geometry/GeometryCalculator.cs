using Concertable.Kernel.Geometry;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Kernel.Services.Geometry;

public sealed class GeometryCalculator : IGeometryCalculator
{
    private readonly IGeometryProvider geometryProvider;

    public GeometryCalculator([FromKeyedServices(GeometryProviderType.Metric)] IGeometryProvider geometryProvider)
    {
        this.geometryProvider = geometryProvider;
    }

    public bool IsWithinRadius(double lat1, double lon1, double lat2, double lon2, int radiusKm)
    {
        var point1 = geometryProvider.CreatePoint(lat1, lon1);
        var point2 = geometryProvider.CreatePoint(lat2, lon2);

        return point1.IsWithinDistance(point2, radiusKm * 1000);
    }
}
