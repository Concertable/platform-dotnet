using System.Linq.Expressions;
using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

internal sealed class NotSpecification<TEntity> : IPredicateSpecification<TEntity> where TEntity : class
{
    private readonly IPredicateSpecification<TEntity> specification;

    public NotSpecification(IPredicateSpecification<TEntity> specification)
    {
        this.specification = specification;
    }

    public Expression<Func<TEntity, bool>> ToExpression() => this.specification.ToExpression().Not();
}

internal sealed class NotSpecification<TEntity, TParams> : IPredicateSpecification<TEntity, TParams> where TEntity : class
{
    private readonly IPredicateSpecification<TEntity, TParams> specification;

    public NotSpecification(IPredicateSpecification<TEntity, TParams> specification)
    {
        this.specification = specification;
    }

    public Expression<Func<TEntity, bool>> ToExpression(TParams @params) => this.specification.ToExpression(@params).Not();
}
