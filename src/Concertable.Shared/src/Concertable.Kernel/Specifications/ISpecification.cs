using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public interface ISpecification<TEntity> where TEntity : class
{
    IReadOnlyList<IncludePath<TEntity>> Includes { get; }
}

public interface ISpecification<TEntity, TResult> : ISpecification<TEntity> where TEntity : class
{
    Expression<Func<TEntity, TResult>> Selector { get; }
}

public interface IOrderedSpecification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    IReadOnlyList<SpecificationOrder<TEntity>> Orders { get; }
}

public interface IOrderedSpecification<TEntity, TResult> : ISpecification<TEntity, TResult>, IOrderedSpecification<TEntity>
    where TEntity : class;

public interface ISpecificationBuilder<TEntity> : IOrderedSpecification<TEntity> where TEntity : class
{
    IncludePath<TEntity> StartInclude(LambdaExpression navigation);

    void AddOrder<TProperty>(Expression<Func<TEntity, TProperty>> keySelector, SpecificationOrderDirection direction);
}
