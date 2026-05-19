using System.Linq.Expressions;

namespace Concertable.DataAccess.Infrastructure.Expressions;

public static class ExpressionExtensions
{
    public static Expression<Func<TEntity, TResult>> Substitute<TEntity, TIn, TResult>(
        this Expression<Func<TEntity, TIn>> selector,
        Expression<Func<TIn, TResult>> condition)
    {
        var body = new ParameterReplacer(condition.Parameters[0], selector.Body)
            .Visit(condition.Body)!;

        return Expression.Lambda<Func<TEntity, TResult>>(body, selector.Parameters[0]);
    }
}
