using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.DataAccess.Application.Specifications;

public interface IDateRangeSpecification<TEntity> : INavigableSpecification<TEntity, DateRange>
    where TEntity : class, IHasDateRange
{
}
