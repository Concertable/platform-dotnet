using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public interface ISpecification;

public interface ISpecification<TEntity> : ISpecification where TEntity : class
{
    IReadOnlyList<LambdaExpression> Includes { get; }
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
