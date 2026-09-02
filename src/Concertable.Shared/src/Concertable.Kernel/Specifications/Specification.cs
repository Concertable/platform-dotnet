using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    private readonly List<IncludePath<TEntity>> includes = [];
    private readonly List<SpecificationOrder<TEntity>> orders = [];

    public IReadOnlyList<IncludePath<TEntity>> Includes => this.includes.AsReadOnly();

    protected IReadOnlyList<SpecificationOrder<TEntity>> RegisteredOrders => this.orders.AsReadOnly();

    protected IncludePath<TEntity> Include(LambdaExpression navigation)
    {
        var path = new IncludePath<TEntity>(navigation);
        this.includes.Add(path);

        return path;
    }

    protected void OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.Order(keySelector, SpecificationOrderDirection.Ascending);

    protected void OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
        this.Order(keySelector, SpecificationOrderDirection.Descending);

    protected void Order<TProperty>(
        Expression<Func<TEntity, TProperty>> keySelector,
        SpecificationOrderDirection direction) =>
        this.orders.Add(SpecificationOrder<TEntity>.Create(keySelector, direction));
}

public abstract class Specification<TEntity, TResult>(Expression<Func<TEntity, TResult>> selector)
    : Specification<TEntity>, ISpecification<TEntity, TResult>
    where TEntity : class
{
    public Expression<Func<TEntity, TResult>> Selector { get; } = selector;
}

public abstract class SpecificationBuilder<TEntity> : Specification<TEntity>, ISpecificationBuilder<TEntity>
    where TEntity : class
{
    public IReadOnlyList<SpecificationOrder<TEntity>> Orders => this.RegisteredOrders;

    IncludePath<TEntity> ISpecificationBuilder<TEntity>.StartInclude(LambdaExpression navigation) =>
        this.Include(navigation);

    void ISpecificationBuilder<TEntity>.AddOrder<TProperty>(
        Expression<Func<TEntity, TProperty>> keySelector,
        SpecificationOrderDirection direction) =>
        this.Order(keySelector, direction);
}

internal sealed class ProjectedSpecification<TEntity, TResult>
    : Specification<TEntity, TResult>, IOrderedSpecification<TEntity, TResult>
    where TEntity : class
{
    public ProjectedSpecification(ISpecification<TEntity> specification, Expression<Func<TEntity, TResult>> selector)
        : base(selector)
    {
        this.Orders = specification is IOrderedSpecification<TEntity> ordered ? ordered.Orders : [];
    }

    public IReadOnlyList<SpecificationOrder<TEntity>> Orders { get; }
}
