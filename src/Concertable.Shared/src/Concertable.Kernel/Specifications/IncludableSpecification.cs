using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

internal sealed class IncludableSpecification<TEntity, TProperty> : IIncludableSpecification<TEntity, TProperty>
    where TEntity : class
{
    private readonly ISpecificationBuilder<TEntity> builder;

    public IncludableSpecification(ISpecificationBuilder<TEntity> builder, IncludePath<TEntity> path)
    {
        this.builder = builder;
        this.Path = path;
    }

    public IncludePath<TEntity> Path { get; }

    public IReadOnlyList<IncludePath<TEntity>> Includes => this.builder.Includes;

    public IReadOnlyList<SpecificationOrder<TEntity>> Orders => this.builder.Orders;

    public IncludePath<TEntity> StartInclude(LambdaExpression navigation) => this.builder.StartInclude(navigation);

    public void AddOrder<TKey>(Expression<Func<TEntity, TKey>> keySelector, SpecificationOrderDirection direction) =>
        this.builder.AddOrder(keySelector, direction);
}
