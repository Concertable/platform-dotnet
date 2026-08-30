using System.Linq.Expressions;
using Concertable.Kernel.Expressions;

namespace Concertable.Kernel.Specifications;

internal sealed class AndSpecification<TEntity> : IPredicateSpecification<TEntity>
    where TEntity : class
{
    private readonly IPredicateSpecification<TEntity> left;
    private readonly Func<Expression<Func<TEntity, bool>>> right;

    public AndSpecification(
        IPredicateSpecification<TEntity> left,
        IPredicateSpecification<TEntity> right)
    {
        this.left = left;
        this.right = right.ToExpression;
    }

    public AndSpecification(
        IPredicateSpecification<TEntity> left,
        Expression<Func<TEntity, bool>> right)
    {
        this.left = left;
        this.right = () => right;
    }

    public Expression<Func<TEntity, bool>> ToExpression() =>
        this.left.ToExpression().And(this.right());
}

internal sealed class AndSpecification<TEntity, TParams> : IPredicateSpecification<TEntity, TParams>
    where TEntity : class
{
    private readonly IPredicateSpecification<TEntity, TParams> left;
    private readonly Func<TParams, Expression<Func<TEntity, bool>>> right;

    public AndSpecification(
        IPredicateSpecification<TEntity, TParams> left,
        IPredicateSpecification<TEntity, TParams> right)
    {
        this.left = left;
        this.right = right.ToExpression;
    }

    public AndSpecification(
        IPredicateSpecification<TEntity, TParams> left,
        Expression<Func<TEntity, bool>> right)
    {
        this.left = left;
        this.right = _ => right;
    }

    public Expression<Func<TEntity, bool>> ToExpression(TParams @params) =>
        this.left.ToExpression(@params).And(this.right(@params));
}
