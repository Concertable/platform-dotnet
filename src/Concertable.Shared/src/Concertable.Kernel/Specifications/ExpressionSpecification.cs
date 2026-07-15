using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

/// <summary>
/// A <see cref="PredicateSpecification{TEntity}"/> over a supplied predicate — the concrete result of
/// composing specifications with <see cref="SpecificationExtensions"/>' And/Or/Not.
/// </summary>
public sealed class ExpressionSpecification<TEntity> : PredicateSpecification<TEntity>
    where TEntity : class
{
    private readonly Expression<Func<TEntity, bool>> predicate;

    public ExpressionSpecification(Expression<Func<TEntity, bool>> predicate) => this.predicate = predicate;

    protected override Expression<Func<TEntity, bool>> Predicate => predicate;
}
