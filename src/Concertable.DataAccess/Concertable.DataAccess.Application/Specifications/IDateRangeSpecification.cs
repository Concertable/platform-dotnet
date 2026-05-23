using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.DataAccess.Application.Specifications;

public interface IDateRangeSpecification<TEntity> : IExpressionSpecification<TEntity, DateRange>
    where TEntity : class, IHasDateRange
{
}
