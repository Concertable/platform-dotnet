using System.Linq.Expressions;
using Concertable.Kernel.Specifications;

namespace Concertable.Kernel.UnitTests.Specifications;

public sealed class SpecificationTests
{
    [Fact]
    public void PredicateSpecification_ToExpression_FiltersByPredicate()
    {
        var sut = new MinAgeSpec(18);
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([20, 30], result.Select(p => p.Age));
    }

    [Fact]
    public void PredicateSpecification_WithParams_ToExpression_FiltersByBuiltPredicate()
    {
        var sut = new AgeAtLeastSpec();
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression(25)).ToArray();

        Assert.Equal([30], result.Select(p => p.Age));
    }


    [Fact]
    public void Not_ComposesTheNegatedSpecification()
    {
        var sut = new MinAgeSpec(18).Not();
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([10], result.Select(p => p.Age));
    }

    [Fact]
    public void And_ComposesBothSpecifications()
    {
        var sut = new MinAgeSpec(18).And(new MaxAgeSpec(25));
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([20], result.Select(p => p.Age));
    }

    [Fact]
    public void Or_ComposesEitherSpecification()
    {
        var sut = new MaxAgeSpec(15).Or(new MinAgeSpec(25));
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([10, 30], result.Select(p => p.Age));
    }

    [Fact]
    public void Composition_ComposesNestedSpecifications()
    {
        var sut = new MinAgeSpec(18)
            .And(new MaxAgeSpec(30).Or(new MinAgeSpec(50)))
            .Not();
        var query = new[] { new Person(10), new Person(20), new Person(40), new Person(60) }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([10, 40], result.Select(p => p.Age));
    }

    [Fact]
    public void ParameterizedComposition_ComposesTheBuiltPredicates()
    {
        var sut = new AgeAtLeastSpec().And(new AgeAtMostSpec());
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression(20)).ToArray();

        Assert.Equal([20], result.Select(p => p.Age));
    }

    [Fact]
    public void IsSatisfiedBy_EvaluatesTheSpecificationForOneEntity()
    {
        var sut = new MinAgeSpec(18);

        Assert.True(sut.IsSatisfiedBy(new Person(18)));
        Assert.False(sut.IsSatisfiedBy(new Person(17)));
    }

    [Fact]
    public void ParameterizedIsSatisfiedBy_EvaluatesTheBuiltPredicateForOneEntity()
    {
        var sut = new AgeAtLeastSpec();

        Assert.True(sut.IsSatisfiedBy(new Person(18), 18));
        Assert.False(sut.IsSatisfiedBy(new Person(17), 18));
    }

    private sealed record Person(int Age);

    private sealed class MaxAgeSpec : PredicateSpecification<Person>
    {
        private readonly int max;
        public MaxAgeSpec(int max) { this.max = max; }
        protected override Expression<Func<Person, bool>> Predicate => p => p.Age <= max;
    }

    private sealed class MinAgeSpec : PredicateSpecification<Person>
    {
        private readonly int min;
        public MinAgeSpec(int min) { this.min = min; }
        protected override Expression<Func<Person, bool>> Predicate => p => p.Age >= min;
    }

    private sealed class AgeAtLeastSpec : PredicateSpecification<Person, int>
    {
        protected override Expression<Func<Person, bool>> BuildPredicate(int min) => p => p.Age >= min;
    }

    private sealed class AgeAtMostSpec : PredicateSpecification<Person, int>
    {
        protected override Expression<Func<Person, bool>> BuildPredicate(int @params) => p => p.Age <= @params;
    }

}
