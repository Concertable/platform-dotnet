using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public sealed record SpecificationOrder<TEntity>(
    LambdaExpression KeySelector,
    SpecificationOrderDirection Direction)
    where TEntity : class;

public enum SpecificationOrderDirection
{
    Ascending,
    Descending
}
