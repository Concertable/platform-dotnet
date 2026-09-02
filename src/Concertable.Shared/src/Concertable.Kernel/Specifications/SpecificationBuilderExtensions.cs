using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public static class SpecificationBuilderExtensions
{
    extension<TEntity>(ISpecificationBuilder<TEntity> builder)
        where TEntity : class
    {
        public IIncludableSpecification<TEntity, TProperty> Include<TProperty>(
            Expression<Func<TEntity, TProperty>> navigation) =>
            new IncludableSpecification<TEntity, TProperty>(builder, builder.StartInclude(navigation));

        public ISpecificationBuilder<TEntity> OrderBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
            builder.WithOrder(keySelector, SpecificationOrderDirection.Ascending);

        public ISpecificationBuilder<TEntity> OrderByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
            builder.WithOrder(keySelector, SpecificationOrderDirection.Descending);

        public ISpecificationBuilder<TEntity> ThenBy<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
            builder.WithOrder(keySelector, SpecificationOrderDirection.Ascending);

        public ISpecificationBuilder<TEntity> ThenByDescending<TProperty>(Expression<Func<TEntity, TProperty>> keySelector) =>
            builder.WithOrder(keySelector, SpecificationOrderDirection.Descending);

        private ISpecificationBuilder<TEntity> WithOrder<TProperty>(
            Expression<Func<TEntity, TProperty>> keySelector,
            SpecificationOrderDirection direction)
        {
            builder.AddOrder(keySelector, direction);
            return builder;
        }
    }

    extension<TEntity, TProperty>(IIncludableSpecification<TEntity, TProperty> includable)
        where TEntity : class
    {
        public IIncludableSpecification<TEntity, TNext> ThenInclude<TNext>(
            Expression<Func<TProperty, TNext>> navigation) =>
            Continue<TEntity, TNext>(includable, includable.Path, navigation);
    }

    extension<TEntity, TElement>(IIncludableSpecification<TEntity, IEnumerable<TElement>> includable)
        where TEntity : class
    {
        public IIncludableSpecification<TEntity, TNext> ThenInclude<TNext>(
            Expression<Func<TElement, TNext>> navigation) =>
            Continue<TEntity, TNext>(includable, includable.Path, navigation);
    }

    extension<TEntity>(ISpecification<TEntity> specification)
        where TEntity : class
    {
        public IOrderedSpecification<TEntity, TResult> Select<TResult>(
            Expression<Func<TEntity, TResult>> selector)
            where TResult : class =>
            new ProjectedSpecification<TEntity, TResult>(specification, selector);
    }

    private static IIncludableSpecification<TEntity, TNext> Continue<TEntity, TNext>(
        ISpecificationBuilder<TEntity> builder,
        IncludePath<TEntity> path,
        LambdaExpression navigation)
        where TEntity : class
    {
        path.Append(navigation);
        return new IncludableSpecification<TEntity, TNext>(builder, path);
    }
}
