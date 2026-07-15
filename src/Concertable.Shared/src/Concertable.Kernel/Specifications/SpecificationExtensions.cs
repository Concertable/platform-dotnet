using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

public static class SpecificationExtensions
{
    public static IPredicateSpecification<T> And<T>(this IPredicateSpecification<T> a, IPredicateSpecification<T> b) where T : class
        => new ExpressionSpecification<T>(a.ToExpression().And(b.ToExpression()));

    public static IPredicateSpecification<T> Or<T>(this IPredicateSpecification<T> a, IPredicateSpecification<T> b) where T : class
        => new ExpressionSpecification<T>(a.ToExpression().Or(b.ToExpression()));

    public static IPredicateSpecification<T> Not<T>(this IPredicateSpecification<T> a) where T : class
        => new ExpressionSpecification<T>(a.ToExpression().Not());
}
