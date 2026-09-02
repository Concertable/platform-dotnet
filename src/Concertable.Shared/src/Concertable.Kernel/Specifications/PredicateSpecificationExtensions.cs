using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public static class PredicateSpecificationExtensions
{
    extension<TEntity>(IPredicateSpecification<TEntity> specification)
        where TEntity : class
    {
        public IPredicateSpecification<TEntity> And(IPredicateSpecification<TEntity> right) =>
            new AndSpecification<TEntity>(specification, right);

        public IPredicateSpecification<TEntity> And(Expression<Func<TEntity, bool>> right) =>
            new AndSpecification<TEntity>(specification, new ExpressionPredicateSpecification<TEntity>(right));

        public IPredicateSpecification<TEntity> Or(IPredicateSpecification<TEntity> right) =>
            new OrSpecification<TEntity>(specification, right);

        public IPredicateSpecification<TEntity> Or(Expression<Func<TEntity, bool>> right) =>
            new OrSpecification<TEntity>(specification, new ExpressionPredicateSpecification<TEntity>(right));

        public IPredicateSpecification<TEntity> Not() =>
            new NotSpecification<TEntity>(specification);

        public IPredicateSpecification<TSource> Via<TSource>(Expression<Func<TSource, TEntity>> navigation)
            where TSource : class =>
            new NavigationSpecification<TSource, TEntity>(specification, navigation);

        public bool IsSatisfiedBy(TEntity entity) =>
            specification.ToExpression().Compile()(entity);
    }

    extension<TEntity, TParams>(IPredicateSpecification<TEntity, TParams> specification)
        where TEntity : class
    {
        public IPredicateSpecification<TEntity, TParams> And(IPredicateSpecification<TEntity, TParams> right) =>
            new AndSpecification<TEntity, TParams>(specification, right);

        public IPredicateSpecification<TEntity, TParams> And(Expression<Func<TEntity, bool>> right) =>
            new AndSpecification<TEntity, TParams>(specification, new ExpressionPredicateSpecification<TEntity, TParams>(right));

        public IPredicateSpecification<TEntity, TParams> Or(IPredicateSpecification<TEntity, TParams> right) =>
            new OrSpecification<TEntity, TParams>(specification, right);

        public IPredicateSpecification<TEntity, TParams> Or(Expression<Func<TEntity, bool>> right) =>
            new OrSpecification<TEntity, TParams>(specification, new ExpressionPredicateSpecification<TEntity, TParams>(right));

        public IPredicateSpecification<TEntity, TParams> Not() =>
            new NotSpecification<TEntity, TParams>(specification);

        public IPredicateSpecification<TSource, TParams> Via<TSource>(Expression<Func<TSource, TEntity>> navigation)
            where TSource : class =>
            new NavigationSpecification<TSource, TEntity, TParams>(specification, navigation);

        public bool IsSatisfiedBy(TEntity entity, TParams @params) =>
            specification.ToExpression(@params).Compile()(entity);
    }
}
