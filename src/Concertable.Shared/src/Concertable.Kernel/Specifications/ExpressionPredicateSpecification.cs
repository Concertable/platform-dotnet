using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

internal sealed class ExpressionPredicateSpecification<TEntity> : IPredicateSpecification<TEntity>
    where TEntity : class
{
    private readonly Expression<Func<TEntity, bool>> predicate;

    public ExpressionPredicateSpecification(Expression<Func<TEntity, bool>> predicate)
    {
        this.predicate = predicate;
    }

    public Expression<Func<TEntity, bool>> ToExpression() => this.predicate;
}

internal sealed class ExpressionPredicateSpecification<TEntity, TParams> : IPredicateSpecification<TEntity, TParams>
    where TEntity : class
{
    private readonly Expression<Func<TEntity, bool>> predicate;

    public ExpressionPredicateSpecification(Expression<Func<TEntity, bool>> predicate)
    {
        this.predicate = predicate;
    }

    public Expression<Func<TEntity, bool>> ToExpression(TParams @params) => this.predicate;
}
