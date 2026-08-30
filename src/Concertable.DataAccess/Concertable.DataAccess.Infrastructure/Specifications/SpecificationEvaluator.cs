using System.Linq.Expressions;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Specifications;

internal static class SpecificationEvaluator
{
    public static IQueryable<TEntity> Apply<TEntity>(this IQueryable<TEntity> query, ISpecification<TEntity> spec)
        where TEntity : class
    {
        foreach (var include in spec.Includes)
            query = query.Include(ToPath(include));

        return query;
    }

    public static IQueryable<TEntity> Apply<TEntity>(this IQueryable<TEntity> query, IOrderedSpecification<TEntity> spec)
        where TEntity : class
    {
        var result = query.Apply((ISpecification<TEntity>)spec);
        IOrderedQueryable<TEntity>? ordered = null;

        foreach (var order in spec.Orders)
        {
            ordered = ApplyOrder(ordered ?? result, order, ordered is null);
            result = ordered;
        }

        return result;
    }

    private static IOrderedQueryable<TEntity> ApplyOrder<TEntity>(
        IQueryable<TEntity> query,
        SpecificationOrder<TEntity> order,
        bool isFirst)
        where TEntity : class
    {
        var methodName = (isFirst, order.Direction) switch
        {
            (true, SpecificationOrderDirection.Ascending) => nameof(Queryable.OrderBy),
            (true, SpecificationOrderDirection.Descending) => nameof(Queryable.OrderByDescending),
            (false, SpecificationOrderDirection.Ascending) => nameof(Queryable.ThenBy),
            _ => nameof(Queryable.ThenByDescending)
        };

        var method = typeof(Queryable).GetMethods()
            .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), order.KeySelector.ReturnType);

        return (IOrderedQueryable<TEntity>)method.Invoke(null, [query, order.KeySelector])!;
    }

    private static string ToPath(LambdaExpression expression)
    {
        var members = new Stack<string>();
        Expression body = expression.Body;

        while (body is MemberExpression member)
        {
            members.Push(member.Member.Name);
            body = member.Expression!;
        }

        return string.Join('.', members);
    }
}
