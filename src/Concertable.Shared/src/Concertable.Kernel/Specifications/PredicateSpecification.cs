using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public abstract class PredicateSpecification<TEntity> : IPredicateSpecification<TEntity>
    where TEntity : class
{
    protected abstract Expression<Func<TEntity, bool>> Predicate { get; }

    public Expression<Func<TEntity, bool>> ToExpression() => Predicate;
}

public abstract class PredicateSpecification<TEntity, TParams> : IPredicateSpecification<TEntity, TParams>
    where TEntity : class
{
    protected abstract Expression<Func<TEntity, bool>> BuildPredicate(TParams @params);

    public Expression<Func<TEntity, bool>> ToExpression(TParams @params) => this.BuildPredicate(@params);
}
