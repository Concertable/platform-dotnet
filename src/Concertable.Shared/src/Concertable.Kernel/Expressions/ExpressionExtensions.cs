using System.Linq.Expressions;

namespace Concertable.Kernel.Expressions;

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

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.OrElse);

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> predicate)
        => Expression.Lambda<Func<T, bool>>(Expression.Not(predicate.Body), predicate.Parameters);

    // Rebind right's parameter onto left's before combining, so the result is a single-parameter tree
    // with no Invoke nodes — the shape EF Core can translate to SQL (naive AndAlso(a.Body, b.Body) can't).
    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> combinator)
    {
        var rightBody = new ParameterReplacer(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(combinator(left.Body, rightBody), left.Parameters[0]);
    }
}
