using System.Linq.Expressions;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Specifications;

public static class QueryableExtensions
{
    extension<TEntity>(IQueryable<TEntity> query)
        where TEntity : class
    {
        public IQueryable<TEntity> Apply(ISpecification<TEntity> specification)
        {
            var result = query;

            foreach (var path in specification.Includes.Select(ToPath).Distinct())
                result = result.Include(path);

            return result;
        }

        public IQueryable<TEntity> ApplyOrders(ISpecification<TEntity> specification) =>
            specification is IOrderedSpecification<TEntity> ordered
                ? query.ApplyOrders(ordered.Orders)
                : query;

        public IQueryable<TEntity> ApplyPagedOrders(ISpecification<TEntity> specification) =>
            specification is IOrderedSpecification<TEntity> { Orders.Count: > 0 } ordered
                ? query.ApplyOrders(ordered.Orders)
                : throw new InvalidOperationException(
                    "A paged query needs a deterministic order. Add OrderBy to the specification before paging it.");

        public IQueryable<TEntity> ApplyOrders(IReadOnlyList<SpecificationOrder<TEntity>> orders)
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

    private static string ToPath<TEntity>(IncludePath<TEntity> include)
        where TEntity : class =>
        string.Join('.', include.Steps.Select(ToSegment));

    private static string ToSegment(LambdaExpression step)
    {
        var members = new Stack<string>();
        Expression body = step.Body;

        while (body is MemberExpression member)
        {
            members.Push(member.Member.Name);
            body = member.Expression
                ?? throw new ArgumentException("An include path must be an instance member expression.", nameof(step));
        }

        if (body is not ParameterExpression || members.Count == 0)
            throw new ArgumentException("An include path must be a member-access chain rooted at the entity.", nameof(step));

        return string.Join('.', members);
    }
}
