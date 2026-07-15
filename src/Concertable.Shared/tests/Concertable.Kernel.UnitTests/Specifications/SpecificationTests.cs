using System.Linq.Expressions;
using Concertable.Kernel.Specifications;

namespace Concertable.Kernel.UnitTests.Specifications;

public sealed class SpecificationTests
{
    [Fact]
    public void PredicateSpecification_Apply_FiltersByPredicate()
    {
        var sut = new MinAgeSpec(18);
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = sut.Apply(query).ToArray();

        Assert.Equal([20, 30], result.Select(p => p.Age));
    }

    [Fact]
    public void PredicateSpecification_WithParams_Apply_FiltersByBuiltPredicate()
    {
        var sut = new AgeAtLeastSpec();
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = sut.Apply(query, 25).ToArray();

        Assert.Equal([30], result.Select(p => p.Age));
    }

    [Fact]
    public void NavigablePredicateSpecification_ApplyVia_RebindsPredicateThroughNavigation()
    {
        var sut = new MinAgeNavigableSpec(18);
        var employees = new[]
        {
            new Employee(new Person(10)),
            new Employee(new Person(20)),
            new Employee(new Person(30))
        }.AsQueryable();

        var result = sut.ApplyVia(employees, e => e.Person).ToArray();

        Assert.Equal([20, 30], result.Select(e => e.Person.Age));
    }

    [Fact]
    public void NavigablePredicateSpecification_WithParams_ApplyVia_RebindsBuiltPredicate()
    {
        var sut = new AgeAtLeastNavigableSpec();
        var employees = new[]
        {
            new Employee(new Person(10)),
            new Employee(new Person(20)),
            new Employee(new Person(30))
        }.AsQueryable();

        var result = sut.ApplyVia(employees, e => e.Person, 25).ToArray();

        Assert.Equal([30], result.Select(e => e.Person.Age));
    }

    [Fact]
    public void Not_ComposesTheNegatedSpecification()
    {
        var sut = new MinAgeSpec(18).Not();
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = sut.Apply(query).ToArray();

        Assert.Equal([10], result.Select(p => p.Age));
    }

    [Fact]
    public void And_ComposesBothSpecifications()
    {
        var sut = new MinAgeSpec(18).And(new MaxAgeSpec(25));
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = sut.Apply(query).ToArray();

        Assert.Equal([20], result.Select(p => p.Age));
    }

    [Fact]
    public void Or_ComposesEitherSpecification()
    {
        var sut = new MaxAgeSpec(15).Or(new MinAgeSpec(25));
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = sut.Apply(query).ToArray();

        Assert.Equal([10, 30], result.Select(p => p.Age));
    }

    private sealed record Person(int Age);
    private sealed record Employee(Person Person);

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

    private sealed class MinAgeNavigableSpec : NavigablePredicateSpecification<Person>
    {
        private readonly int min;
        public MinAgeNavigableSpec(int min) { this.min = min; }
        protected override Expression<Func<Person, bool>> Predicate => p => p.Age >= min;
    }

    private sealed class AgeAtLeastNavigableSpec : NavigablePredicateSpecification<Person, int>
    {
        protected override Expression<Func<Person, bool>> BuildPredicate(int min) => p => p.Age >= min;
    }
}
