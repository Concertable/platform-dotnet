using System.Linq.Expressions;
using Concertable.Shared;

namespace Concertable.DataAccess.Specifications;

public interface IDateRangeSpecification<TEntity> where TEntity : class, IHasDateRange
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, DateRange range);

    IQueryable<TParent> ApplyExpression<TParent>(
        IQueryable<TParent> query,
        Expression<Func<TParent, TEntity>> navigation,
        DateRange range);
}
