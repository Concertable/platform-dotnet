using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public static class IncludableSpecificationExtensions
{
    extension<TEntity, TProperty>(IIncludableSpecification<TEntity, TProperty> includable)
        where TEntity : class
    {
        public IIncludableSpecification<TEntity, TNext> ThenInclude<TNext>(
            Expression<Func<TProperty, TNext>> navigation) =>
            Continue<TEntity, TNext>(includable.Path, navigation);
    }

    extension<TEntity, TElement>(IIncludableSpecification<TEntity, IEnumerable<TElement>> includable)
        where TEntity : class
    {
        public IIncludableSpecification<TEntity, TNext> ThenInclude<TNext>(
            Expression<Func<TElement, TNext>> navigation) =>
            Continue<TEntity, TNext>(includable.Path, navigation);
    }

    private static IIncludableSpecification<TEntity, TNext> Continue<TEntity, TNext>(
        IncludePath<TEntity> path,
        LambdaExpression navigation)
        where TEntity : class
    {
        path.Append(navigation);
        return new IncludableSpecification<TEntity, TNext>(path);
    }
}
