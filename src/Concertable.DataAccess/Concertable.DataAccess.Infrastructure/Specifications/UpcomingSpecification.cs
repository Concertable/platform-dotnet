using System.Linq.Expressions;
using Concertable.DataAccess.Application.Specifications;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.DataAccess.Infrastructure.Specifications;

internal class UpcomingSpecification<TEntity>
    : PredicateExpressionSpecification<TEntity>, IUpcomingSpecification<TEntity>
    where TEntity : class, IHasDateRange
{
    private readonly TimeProvider timeProvider;

    public UpcomingSpecification(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    protected override Expression<Func<TEntity, bool>> Predicate
    {
        get
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            return e => e.Period.End > now;
        }
    }
}
