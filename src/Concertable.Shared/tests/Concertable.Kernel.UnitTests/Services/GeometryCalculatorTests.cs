using Concertable.Kernel.Services.Geometry;
using NetTopologySuite;

namespace Concertable.Kernel.UnitTests.Services;

public sealed class GeometryCalculatorTests
{
    private readonly GeometryCalculator sut;

    public GeometryCalculatorTests()
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);
        var geometryProvider = new MetricGeometryProvider(geometryFactory);
        sut = new GeometryCalculator(geometryProvider);
    }

    [Fact]
    public void IsWithinRadius_ShouldReturnTrue_WhenPointsAreWithinRadius()
    {
        Assert.True(sut.IsWithinRadius(51.5074, -0.1278, 51.5154, -0.1278, 10));
    }

    [Fact]
    public void IsWithinRadius_ShouldReturnFalse_WhenPointsAreOutsideRadius()
    {
        Assert.False(sut.IsWithinRadius(51.5074, -0.1278, 53.4808, -2.2426, 10));
    }

    [Fact]
    public void IsWithinRadius_ShouldReturnTrue_WhenSamePoint()
    {
        Assert.True(sut.IsWithinRadius(51.5074, -0.1278, 51.5074, -0.1278, 1));
    }
}
