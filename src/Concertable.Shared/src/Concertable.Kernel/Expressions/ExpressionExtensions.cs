using System.Linq.Expressions;

namespace Concertable.Kernel.Expressions;

public static class ExpressionExtensions
{
    extension<TEntity, TIn, TResult>(Expression<Func<TEntity, TIn>> selector)
    {
        public Expression<Func<TEntity, TResult>> Substitute(
            Expression<Func<TIn, TResult>> condition)
        {
            var body = new ParameterReplacer(condition.Parameters[0], selector.Body)
                .Visit(condition.Body)!;

            return Expression.Lambda<Func<TEntity, TResult>>(body, selector.Parameters[0]);
        }
    }

    extension<TEntity>(Expression<Func<TEntity, bool>> predicate)
    {
        public Expression<Func<TEntity, bool>> And(Expression<Func<TEntity, bool>> right) =>
            Combine(predicate, right, Expression.AndAlso);

        public Expression<Func<TEntity, bool>> Or(Expression<Func<TEntity, bool>> right) =>
            Combine(predicate, right, Expression.OrElse);

        public Expression<Func<TEntity, bool>> Not() =>
            Expression.Lambda<Func<TEntity, bool>>(
                Expression.Not(predicate.Body),
                predicate.Parameters);
    }

    private static Expression<Func<TEntity, bool>> Combine<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right,
        Func<Expression, Expression, BinaryExpression> combinator)
    {
        var rightBody = new ParameterReplacer(right.Parameters[0], left.Parameters[0])
            .Visit(right.Body)!;

        return Expression.Lambda<Func<TEntity, bool>>(
            combinator(left.Body, rightBody),
            left.Parameters[0]);
    }
}
