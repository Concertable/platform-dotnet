using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public abstract class PredicateSpecification<TEntity> : IPredicateSpecification<TEntity>
    where TEntity : class
{
    protected abstract Expression<Func<TEntity, bool>> Predicate { get; }

    public Expression<Func<TEntity, bool>> ToExpression() => Predicate;

    public IQueryable<TEntity> Apply(IQueryable<TEntity> query)
        => query.Where(Predicate);
}

public abstract class PredicateSpecification<TEntity, TParams> : ISpecification<TEntity, TParams>
    where TEntity : class
{
    protected abstract Expression<Func<TEntity, bool>> BuildPredicate(TParams @params);

    public IQueryable<TEntity> Apply(IQueryable<TEntity> query, TParams @params)
        => query.Where(BuildPredicate(@params));
}
