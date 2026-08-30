using System.Linq.Expressions;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Specifications;

public static class QueryableExtensions
{
    extension<TEntity>(IQueryable<TEntity> query)
        where TEntity : class
    {
        public IQueryable<TEntity> Apply(ISpecification<TEntity> specification) =>
            ApplyIncludes(query, specification);

        public IQueryable<TEntity> Apply(IOrderedSpecification<TEntity> specification) =>
            ApplyIncludes(query, specification).ApplyOrders(specification.Orders);

        public IQueryable<TEntity> ApplyOrders(
            IReadOnlyList<SpecificationOrder<TEntity>> orders)
        {
            var result = query;
            IOrderedQueryable<TEntity>? ordered = null;

            foreach (var order in orders)
            {
                ordered = ordered is null
                    ? ApplyOrder(result, order)
                    : ApplyThenOrder(ordered, order);
                result = ordered;
            }

            return result;
        }
    }

    private static IQueryable<TEntity> ApplyIncludes<TEntity>(
        IQueryable<TEntity> query,
        ISpecification<TEntity> specification)
        where TEntity : class
    {
        foreach (var include in specification.Includes)
            query = query.Include(ToPath(include));

        return query;
    }

    private static IOrderedQueryable<TEntity> ApplyOrder<TEntity>(
        IQueryable<TEntity> query,
        SpecificationOrder<TEntity> order)
        where TEntity : class
    {
        return order.Direction switch
        {
            SpecificationOrderDirection.Ascending => query.OrderBy(order.KeySelector),
            _ => query.OrderByDescending(order.KeySelector)
        };
    }

    private static IOrderedQueryable<TEntity> ApplyThenOrder<TEntity>(
        IOrderedQueryable<TEntity> query,
        SpecificationOrder<TEntity> order)
        where TEntity : class
    {
        return order.Direction switch
        {
            SpecificationOrderDirection.Ascending => query.ThenBy(order.KeySelector),
            _ => query.ThenByDescending(order.KeySelector)
        };
    }

    private static string ToPath(LambdaExpression expression)
    {
        var members = new Stack<string>();
        Expression body = expression.Body;

        while (body is MemberExpression member)
        {
            members.Push(member.Member.Name);
            body = member.Expression
                ?? throw new ArgumentException("An include path must be an instance member expression.", nameof(expression));
        }

        if (body is not ParameterExpression || members.Count == 0)
            throw new ArgumentException("An include path must be a member-access chain rooted at the entity.", nameof(expression));

        return string.Join('.', members);
    }
}
