using Concertable.Kernel.Expressions;
using NetTopologySuite.Geometries;
using System.Linq.Expressions;

namespace Concertable.Kernel.UnitTests.Expressions;

public sealed class ExpressionExtensionsTests
{
    [Fact]
    public void Substitute_ShouldInlineSelector_IntoCondition()
    {
        Expression<Func<string, int>> selector = s => s.Length;
        Expression<Func<int, bool>> condition = n => n > 5;

        var result = selector.Substitute(condition);
        var compiled = result.Compile();

        Assert.True(compiled("toolong"));
        Assert.False(compiled("hi"));
    }

    [Fact]
    public void Substitute_ShouldHandleNullableSelector()
    {
        Expression<Func<string?, int?>> selector = s => s == null ? null : (int?)s.Length;
        Expression<Func<int?, bool>> condition = n => n != null && n > 3;

        var result = selector.Substitute(condition);
        var compiled = result.Compile();

        Assert.True(compiled("test"));
        Assert.False(compiled(null));
    }

    [Fact]
    public void Substitute_ShouldProduceCorrectParameterInOutput()
    {
        Expression<Func<string, int>> selector = s => s.Length;
        Expression<Func<int, bool>> condition = n => n == 3;

        var result = selector.Substitute(condition);

        Assert.Single(result.Parameters);
        Assert.Equal(typeof(string), result.Parameters[0].Type);
        Assert.Equal(typeof(bool), result.ReturnType);
    }

    [Fact]
    public void Substitute_ShouldWorkWithPointDistance()
    {
        var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var center = factory.CreatePoint(new Coordinate(0, 0));

        Expression<Func<(Point? Location, string Name), Point?>> selector = x => x.Location;
        Expression<Func<Point?, bool>> condition = p => p != null && p.Distance(center) <= 1000;

        var result = selector.Substitute(condition);
        var compiled = result.Compile();

        var nearPoint = factory.CreatePoint(new Coordinate(0.001, 0.001));
        Assert.True(compiled((nearPoint, "test")));
        Assert.False(compiled((null, "test")));
    }
}
