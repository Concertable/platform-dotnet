using System.Linq.Expressions;
using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

public static class SpecificationExtensions
{
    extension<TEntity>(IPredicateSpecification<TEntity> specification)
        where TEntity : class
    {
        public IPredicateSpecification<TEntity> And(IPredicateSpecification<TEntity> right) =>
            new AndSpecification<TEntity>(specification, right);

        public IPredicateSpecification<TEntity> And(Expression<Func<TEntity, bool>> right) =>
            new AndSpecification<TEntity>(specification, right);

        public IPredicateSpecification<TEntity> Or(IPredicateSpecification<TEntity> right) =>
            new OrSpecification<TEntity>(specification, right);

        public IPredicateSpecification<TEntity> Not() =>
            new NotSpecification<TEntity>(specification);

        public bool IsSatisfiedBy(TEntity entity) =>
            specification.ToExpression().Compile()(entity);
    }

    extension<TEntity, TParams>(IPredicateSpecification<TEntity, TParams> specification)
        where TEntity : class
    {
        public IPredicateSpecification<TEntity, TParams> And(
            IPredicateSpecification<TEntity, TParams> right) =>
            new AndSpecification<TEntity, TParams>(specification, right);

        public IPredicateSpecification<TEntity, TParams> And(
            Expression<Func<TEntity, bool>> right) =>
            new AndSpecification<TEntity, TParams>(specification, right);

        public IPredicateSpecification<TEntity, TParams> Or(
            IPredicateSpecification<TEntity, TParams> right) =>
            new OrSpecification<TEntity, TParams>(specification, right);

        public IPredicateSpecification<TEntity, TParams> Not() =>
            new NotSpecification<TEntity, TParams>(specification);

        public bool IsSatisfiedBy(TEntity entity, TParams @params) =>
            specification.ToExpression(@params).Compile()(entity);
    }

    extension<TNavigation>(IPredicateSpecification<TNavigation> specification)
        where TNavigation : class
    {
        public Expression<Func<TSource, bool>> Via<TSource>(
            Expression<Func<TSource, TNavigation>> navigation)
            where TSource : class =>
            navigation.Substitute(specification.ToExpression());
    }

    extension<TNavigation, TParams>(IPredicateSpecification<TNavigation, TParams> specification)
        where TNavigation : class
    {
        public Expression<Func<TSource, bool>> Via<TSource>(
            Expression<Func<TSource, TNavigation>> navigation,
            TParams @params)
            where TSource : class =>
            navigation.Substitute(specification.ToExpression(@params));
    }
}
