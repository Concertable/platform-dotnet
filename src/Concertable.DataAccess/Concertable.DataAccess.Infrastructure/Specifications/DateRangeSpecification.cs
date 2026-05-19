using System.Linq.Expressions;
using Concertable.DataAccess.Specifications;
using Concertable.Shared;
using Concertable.DataAccess.Infrastructure.Expressions;

namespace Concertable.DataAccess.Infrastructure.Specifications;

internal class DateRangeSpecification<TEntity> : IDateRangeSpecification<TEntity>
    where TEntity : class, IHasDateRange
{
    public IQueryable<TEntity> Apply(IQueryable<TEntity> query, DateRange range)
        => query.Where(BuildPredicate(range));

    public IQueryable<TParent> ApplyExpression<TParent>(
        IQueryable<TParent> query,
        Expression<Func<TParent, TEntity>> navigation,
        DateRange range)
        => query.Where(navigation.Substitute(BuildPredicate(range)));

    private static Expression<Func<TEntity, bool>> BuildPredicate(DateRange range)
        => e => e.Period.Start < range.End && e.Period.End > range.Start;
}
