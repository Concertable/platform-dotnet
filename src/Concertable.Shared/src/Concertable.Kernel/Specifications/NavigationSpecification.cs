using System.Linq.Expressions;
using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

internal sealed class NavigationSpecification<TSource, TNavigation> : IPredicateSpecification<TSource>
    where TSource : class
    where TNavigation : class
{
    private readonly IPredicateSpecification<TNavigation> specification;
    private readonly Expression<Func<TSource, TNavigation>> navigation;

    public NavigationSpecification(
        IPredicateSpecification<TNavigation> specification,
        Expression<Func<TSource, TNavigation>> navigation)
    {
        this.specification = specification;
        this.navigation = navigation;
    }

    public Expression<Func<TSource, bool>> ToExpression() =>
        this.navigation.Substitute(this.specification.ToExpression());
}

internal sealed class NavigationSpecification<TSource, TNavigation, TParams> : IPredicateSpecification<TSource, TParams>
    where TSource : class
    where TNavigation : class
{
    private readonly IPredicateSpecification<TNavigation, TParams> specification;
    private readonly Expression<Func<TSource, TNavigation>> navigation;

    public NavigationSpecification(
        IPredicateSpecification<TNavigation, TParams> specification,
        Expression<Func<TSource, TNavigation>> navigation)
    {
        this.specification = specification;
        this.navigation = navigation;
    }

    public Expression<Func<TSource, bool>> ToExpression(TParams @params) =>
        this.navigation.Substitute(this.specification.ToExpression(@params));
}
