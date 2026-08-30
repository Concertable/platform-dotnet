using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    private readonly List<LambdaExpression> includes = [];
    private readonly List<SpecificationOrder<TEntity>> orders = [];

    public IReadOnlyList<LambdaExpression> Includes => this.includes.AsReadOnly();
    protected IReadOnlyList<SpecificationOrder<TEntity>> RegisteredOrders => this.orders.AsReadOnly();

    protected void Include<TProperty>(Expression<Func<TEntity, TProperty>> navigation) => this.includes.Add(navigation);

    protected void OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Ascending);

    protected void OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Descending);

    protected void ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Ascending);

    protected void ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.AddOrder(keySelector, SpecificationOrderDirection.Descending);

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
}
