using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public static class ValueSpecificationExtensions
{
    extension<TEntity>(ISpecification<TEntity> specification)
        where TEntity : class
    {
        public IOrderedSpecification<TEntity, TResult?> Select<TResult>(
            Expression<Func<TEntity, TResult>> selector)
            where TResult : struct =>
            new ProjectedSpecification<TEntity, TResult?>(specification, ToNullable(selector));
    }

    private static Expression<Func<TEntity, TResult?>> ToNullable<TEntity, TResult>(
        Expression<Func<TEntity, TResult>> selector)
        where TEntity : class
        where TResult : struct =>
        Expression.Lambda<Func<TEntity, TResult?>>(
            Expression.Convert(selector.Body, typeof(TResult?)),
            selector.Parameters);
}
