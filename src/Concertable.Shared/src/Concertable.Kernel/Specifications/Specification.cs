using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    private readonly List<IncludePath<TEntity>> includes = [];
    private readonly List<SpecificationOrder<TEntity>> orders = [];

    public IReadOnlyList<IncludePath<TEntity>> Includes => this.includes.AsReadOnly();

    protected IReadOnlyList<SpecificationOrder<TEntity>> RegisteredOrders => this.orders.AsReadOnly();

    protected IIncludableSpecification<TEntity, TProperty> Include<TProperty>(
        Expression<Func<TEntity, TProperty>> navigation)
    {
        this.EnsureIncludable();

        var path = new IncludePath<TEntity>(navigation);
        this.includes.Add(path);

        return new IncludableSpecification<TEntity, TProperty>(path);
    }

    protected void OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Ascending);

    protected void OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Descending);

    protected void ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Ascending);

    protected void ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Descending);

    private protected virtual void EnsureIncludable()
    {
    }

    private void AddOrder<TProperty>(Expression<Func<TEntity, TProperty>> keySelector, SpecificationOrderDirection direction)
    {
        this.orders.Add(SpecificationOrder<TEntity>.Create(keySelector, direction));
    }
}

public abstract class Specification<TEntity, TResult>(Expression<Func<TEntity, TResult>> selector)
    : Specification<TEntity>, ISpecification<TEntity, TResult>
    where TEntity : class
{
    public Expression<Func<TEntity, TResult>> Selector { get; } = selector;

    private protected override void EnsureIncludable() =>
        throw new InvalidOperationException(
            "A projected specification cannot register includes; EF Core builds a projection's joins from its selector.");
}
