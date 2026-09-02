using Concertable.Kernel.Specifications;
using Concertable.Kernel.ValueObjects;

namespace Concertable.DataAccess.Application.Specifications;

public interface IUpcomingSpecification<TEntity> : IPredicateSpecification<TEntity>
    where TEntity : class, IHasDateRange
{ }
