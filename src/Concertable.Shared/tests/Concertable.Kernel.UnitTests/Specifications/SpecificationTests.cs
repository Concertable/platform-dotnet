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
    public void And_SpecificationAndExpression_ComposesThePredicate()
    {
        var sut = new AgeAtLeastSpec().And(person => person.Age <= 25);
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression(18)).ToArray();

        Assert.Equal([20], result.Select(person => person.Age));
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

    [Fact]
    public void Or_SpecificationAndExpression_ComposesThePredicate()
    {
        var sut = new MaxAgeSpec(15).Or(person => person.Age >= 25);
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([10, 30], result.Select(person => person.Age));
    }

    [Fact]
    public void ParameterizedOr_SpecificationAndExpression_ComposesThePredicate()
    {
        var sut = new AgeAtLeastSpec().Or(person => person.Age == 10);
        var query = new[] { new Person(10), new Person(20), new Person(30) }.AsQueryable();

        var result = query.Where(sut.ToExpression(25)).ToArray();

        Assert.Equal([10, 30], result.Select(person => person.Age));
    }

    [Fact]
    public void Via_AdaptsThePredicateThroughTheNavigation()
    {
        var sut = new MinAgeSpec(18).Via((Household household) => household.Head);
        var query = new[]
        {
            new Household(new Person(10)),
            new Household(new Person(20))
        }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([20], result.Select(household => household.Head.Age));
    }

    [Fact]
    public void Via_RemainsComposableBeforeMaterialization()
    {
        var sut = new MinAgeSpec(30)
            .Via((Household household) => household.Head)
            .Or(new MaxAgeSpec(12).Via((Household household) => household.Head))
            .And(household => household.Head.Age != 12);
        var query = new[]
        {
            new Household(new Person(10)),
            new Household(new Person(12)),
            new Household(new Person(20)),
            new Household(new Person(40))
        }.AsQueryable();

        var result = query.Where(sut.ToExpression()).ToArray();

        Assert.Equal([10, 40], result.Select(household => household.Head.Age));
    }

    [Fact]
    public void ParameterizedVia_AdaptsTheBuiltPredicateThroughTheNavigation()
    {
        var sut = new AgeAtLeastSpec().Via((Household household) => household.Head);
        var query = new[]
        {
            new Household(new Person(10)),
            new Household(new Person(30))
        }.AsQueryable();

        var result = query.Where(sut.ToExpression(25)).ToArray();

        Assert.Equal([30], result.Select(household => household.Head.Age));
    }

    [Fact]
    public void Via_MaterializesOneParameterWithoutInvocation()
    {
        var sut = new MinAgeSpec(18)
            .Via((Household household) => household.Head)
            .Or(new MaxAgeSpec(5).Via((Household household) => household.Head));

        var expression = sut.ToExpression();

        Assert.Single(expression.Parameters);
        Assert.False(new InvocationDetector().Detects(expression));
    }

    [Fact]
    public void Include_ChainsThenIncludeThroughAReferenceNavigation()
    {
        var sut = new HouseholdSpec().Include(household => household.Head).ThenInclude(person => person.City);

        var steps = Assert.Single(sut.Includes).Steps;

        Assert.Equal(2, steps.Count);
    }

    [Fact]
    public void Include_ChainsThenIncludeThroughACollectionNavigation()
    {
        var sut = new HouseholdSpec().Include(household => household.Members).ThenInclude(person => person.City);

        var steps = Assert.Single(sut.Includes).Steps;

        Assert.Equal(2, steps.Count);
    }

    [Fact]
    public void Include_IsAdditiveAcrossFluentCalls()
    {
        var sut = new HouseholdSpec()
            .Include(household => household.Head)
            .Include(household => household.Members);

        Assert.Equal(2, sut.Includes.Count);
    }

    [Fact]
    public void Select_CarriesOrdersAndDropsIncludes()
    {
        var sut = new HouseholdSpec()
            .Include(household => household.Head)
            .OrderBy(household => household.Head.Age)
            .Select(household => household.Head.Age);

        Assert.Single(sut.Orders);
        Assert.Empty(sut.Includes);
    }

    [Fact]
    public void OrderBy_ThenBy_PreservesSequence()
    {
        var sut = new HouseholdSpec()
            .OrderByDescending(household => household.Head.Age)
            .ThenBy(household => household.Members.Count);

        Assert.Equal(
            [SpecificationOrderDirection.Descending, SpecificationOrderDirection.Ascending],
            sut.Orders.Select(order => order.Direction));
    }

    private sealed record Person(int Age)
    {
        public City? City { get; init; }
    }

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
        protected override Expression<Func<Person, bool>> Predicate(int min) => p => p.Age >= min;
    }

    private sealed class AgeAtMostSpec : PredicateSpecification<Person, int>
    {
        protected override Expression<Func<Person, bool>> Predicate(int @params) => p => p.Age <= @params;
    }

    private sealed class Household
    {
        public Household(Person head)
        {
            this.Head = head;
            this.Members = [head];
        }

        public Person Head { get; }
        public ICollection<Person> Members { get; }
    }

    private sealed record City(string Name);

    private sealed class HouseholdSpec : SpecificationBuilder<Household>;

    private sealed class InvocationDetector : ExpressionVisitor
    {
        private bool found;

        public bool Detects(Expression expression)
        {
            this.found = false;
            this.Visit(expression);
            return this.found;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            this.found = true;
            return base.VisitInvocation(node);
        }
    }
}
