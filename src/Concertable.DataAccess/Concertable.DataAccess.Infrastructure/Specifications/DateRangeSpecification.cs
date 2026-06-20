using System.Linq.Expressions;
using Concertable.DataAccess.Application.Specifications;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.DataAccess.Infrastructure.Specifications;

internal sealed class DateRangeSpecification<TEntity>
    : NavigablePredicateSpecification<TEntity, DateRange>, IDateRangeSpecification<TEntity>
    where TEntity : class, IHasDateRange
{
    protected override Expression<Func<TEntity, bool>> BuildPredicate(DateRange range)
        => e => e.Period.Start < range.End && e.Period.End > range.Start;
}
