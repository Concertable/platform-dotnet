using System.Linq.Expressions;
using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

public interface IPredicateSpecification<TEntity> : ISpecification
    where TEntity : class
{
    Expression<Func<TEntity, bool>> ToExpression();

    Expression<Func<TSource, bool>> ToExpression<TSource>(
        Expression<Func<TSource, TEntity>> navigation)
        where TSource : class =>
        navigation.Substitute(this.ToExpression());
}

public interface IPredicateSpecification<TEntity, TParams> : ISpecification
    where TEntity : class
{
    Expression<Func<TEntity, bool>> ToExpression(TParams @params);

    Expression<Func<TSource, bool>> ToExpression<TSource>(
        Expression<Func<TSource, TEntity>> navigation,
        TParams @params)
        where TSource : class =>
        navigation.Substitute(this.ToExpression(@params));
}
