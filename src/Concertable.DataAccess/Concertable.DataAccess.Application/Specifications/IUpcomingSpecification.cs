using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.DataAccess.Application.Specifications;

public interface IUpcomingSpecification<TEntity> : IExpressionSpecification<TEntity>
    where TEntity : class, IHasDateRange
{
}
