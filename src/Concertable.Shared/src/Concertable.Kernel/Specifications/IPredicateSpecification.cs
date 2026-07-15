using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public interface IPredicateSpecification<TEntity> : ISpecification<TEntity>
    where TEntity : class
{
    Expression<Func<TEntity, bool>> ToExpression();
}
