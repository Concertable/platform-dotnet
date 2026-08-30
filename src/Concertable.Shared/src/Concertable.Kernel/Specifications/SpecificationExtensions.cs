using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

public static class SpecificationExtensions
{
    public static IPredicateSpecification<T> And<T>(this IPredicateSpecification<T> left, IPredicateSpecification<T> right) where T : class
        => new AndSpecification<T>(left, right);

    public static IPredicateSpecification<T> Or<T>(this IPredicateSpecification<T> left, IPredicateSpecification<T> right) where T : class
        => new OrSpecification<T>(left, right);

    public static IPredicateSpecification<T> Not<T>(this IPredicateSpecification<T> specification) where T : class
        => new NotSpecification<T>(specification);

    public static bool IsSatisfiedBy<T>(this IPredicateSpecification<T> specification, T entity) where T : class
        => specification.ToExpression().Compile()(entity);

    public static IPredicateSpecification<T, TParams> And<T, TParams>(this IPredicateSpecification<T, TParams> left, IPredicateSpecification<T, TParams> right) where T : class
        => new AndSpecification<T, TParams>(left, right);

    public static IPredicateSpecification<T, TParams> Or<T, TParams>(this IPredicateSpecification<T, TParams> left, IPredicateSpecification<T, TParams> right) where T : class
        => new OrSpecification<T, TParams>(left, right);

    public static IPredicateSpecification<T, TParams> Not<T, TParams>(this IPredicateSpecification<T, TParams> specification) where T : class
        => new NotSpecification<T, TParams>(specification);

    public static bool IsSatisfiedBy<T, TParams>(this IPredicateSpecification<T, TParams> specification, T entity, TParams @params) where T : class
        => specification.ToExpression(@params).Compile()(entity);
}
