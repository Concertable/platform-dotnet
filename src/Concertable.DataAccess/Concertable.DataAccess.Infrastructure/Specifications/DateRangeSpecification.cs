using System.Linq.Expressions;
using Concertable.DataAccess.Application.Specifications;
using Concertable.Kernel.Specifications;
using Concertable.Kernel.ValueObjects;

namespace Concertable.DataAccess.Infrastructure.Specifications;

internal sealed class DateRangeSpecification<TEntity>
    : PredicateSpecification<TEntity, DateRange>, IDateRangeSpecification<TEntity>
    where TEntity : class, IHasDateRange
{
    protected override Expression<Func<TEntity, bool>> Predicate(DateRange range)
        => e => e.Period.Start < range.End && e.Period.End > range.Start;
}
