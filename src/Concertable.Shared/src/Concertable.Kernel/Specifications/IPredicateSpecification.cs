using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public interface IPredicateSpecification<TEntity> : ISpecification
    where TEntity : class
{
    Expression<Func<TEntity, bool>> ToExpression();
}

public interface IPredicateSpecification<TEntity, TParams> : ISpecification
    where TEntity : class
{
    Expression<Func<TEntity, bool>> ToExpression(TParams @params);
}
