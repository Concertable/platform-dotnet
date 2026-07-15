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

    [Fact]
    public void And_ShouldRequireBothPredicates()
    {
        Expression<Func<int, bool>> positive = n => n > 0;
        Expression<Func<int, bool>> even = n => n % 2 == 0;

        var compiled = positive.And(even).Compile();

        Assert.True(compiled(2));
        Assert.False(compiled(3));
        Assert.False(compiled(-2));
    }

    [Fact]
    public void Or_ShouldRequireEitherPredicate()
    {
        Expression<Func<int, bool>> negative = n => n < 0;
        Expression<Func<int, bool>> even = n => n % 2 == 0;

        var compiled = negative.Or(even).Compile();

        Assert.True(compiled(-3));
        Assert.True(compiled(4));
        Assert.False(compiled(3));
    }

    [Fact]
    public void Not_ShouldInvertPredicate()
    {
        Expression<Func<int, bool>> even = n => n % 2 == 0;

        var compiled = even.Not().Compile();

        Assert.False(compiled(2));
        Assert.True(compiled(3));
    }

    [Theory]
    [MemberData(nameof(Combinators))]
    public void Combinators_ShouldProduceSingleParameterTreeWithoutInvoke(
        Expression<Func<int, bool>> combined)
    {
        // The EF-translatability guard: naive combination leaves two parameters or Invoke nodes, which
        // EF Core cannot translate to SQL. Unifying parameters (via ParameterReplacer) avoids both.
        Assert.Single(combined.Parameters);
        Assert.False(new InvocationDetector().Contains(combined));
    }

    public static TheoryData<Expression<Func<int, bool>>> Combinators()
    {
        Expression<Func<int, bool>> a = n => n > 0;
        Expression<Func<int, bool>> b = n => n % 2 == 0;
        return new TheoryData<Expression<Func<int, bool>>> { a.And(b), a.Or(b), a.Not() };
    }

    private sealed class InvocationDetector : ExpressionVisitor
    {
        private bool found;

        public bool Contains(Expression expression)
        {
            found = false;
            Visit(expression);
            return found;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            found = true;
            return base.VisitInvocation(node);
        }
    }
}
