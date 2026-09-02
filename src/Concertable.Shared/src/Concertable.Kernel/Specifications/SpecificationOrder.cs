using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public sealed record SpecificationOrder<TEntity>(
    Expression<Func<TEntity, object?>> KeySelector,
    SpecificationOrderDirection Direction)
    where TEntity : class
{
    public static SpecificationOrder<TEntity> Create<TProperty>(
        Expression<Func<TEntity, TProperty>> keySelector,
        SpecificationOrderDirection direction) =>
        new(
            Expression.Lambda<Func<TEntity, object?>>(
                Expression.Convert(keySelector.Body, typeof(object)),
                keySelector.Parameters),
            direction);
}

public enum SpecificationOrderDirection
{
    Ascending,
    Descending
}
