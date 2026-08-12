using Concertable.Kernel.Specifications;
using Concertable.Kernel.ValueObjects;

namespace Concertable.DataAccess.Application.Specifications;

public interface IDateRangeSpecification<TEntity> : INavigableSpecification<TEntity, DateRange>
    where TEntity : class, IHasDateRange
{ }
