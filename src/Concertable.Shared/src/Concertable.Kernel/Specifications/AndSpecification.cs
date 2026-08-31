using System.Linq.Expressions;
using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

internal sealed class AndSpecification<TEntity> : IPredicateSpecification<TEntity>
    where TEntity : class
{
    private readonly IPredicateSpecification<TEntity> left;
    private readonly IPredicateSpecification<TEntity> right;

    public AndSpecification(IPredicateSpecification<TEntity> left, IPredicateSpecification<TEntity> right)
    {
        this.left = left;
        this.right = right;
    }

    public Expression<Func<TEntity, bool>> ToExpression() =>
        this.left.ToExpression().And(this.right.ToExpression());
}

internal sealed class AndSpecification<TEntity, TParams> : IPredicateSpecification<TEntity, TParams>
    where TEntity : class
{
    private readonly IPredicateSpecification<TEntity, TParams> left;
    private readonly IPredicateSpecification<TEntity, TParams> right;

    public AndSpecification(
        IPredicateSpecification<TEntity, TParams> left,
        IPredicateSpecification<TEntity, TParams> right)
    {
        this.left = left;
        this.right = right;
    }

    public Expression<Func<TEntity, bool>> ToExpression(TParams @params) =>
        this.left.ToExpression(@params).And(this.right.ToExpression(@params));
}
