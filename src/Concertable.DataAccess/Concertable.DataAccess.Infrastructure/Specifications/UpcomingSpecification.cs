using System.Linq.Expressions;
using Concertable.DataAccess.Infrastructure.Expressions;
using Concertable.DataAccess.Application.Specifications;
using Concertable.Kernel;

namespace Concertable.DataAccess.Infrastructure.Specifications;

internal class UpcomingSpecification<TEntity> : IUpcomingSpecification<TEntity>
    where TEntity : class, IHasDateRange
{
    private readonly TimeProvider timeProvider;

    public UpcomingSpecification(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public IQueryable<TEntity> Apply(IQueryable<TEntity> query)
        => query.Where(BuildPredicate());

    public IQueryable<TParent> ApplyExpression<TParent>(
        IQueryable<TParent> query,
        Expression<Func<TParent, TEntity>> navigation)
        => query.Where(navigation.Substitute(BuildPredicate()));

    private Expression<Func<TEntity, bool>> BuildPredicate()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return e => e.Period.End > now;
    }
}
