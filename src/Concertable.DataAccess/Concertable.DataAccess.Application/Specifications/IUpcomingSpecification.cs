using System.Linq.Expressions;
using Concertable.Kernel;

namespace Concertable.DataAccess.Application.Specifications;

public interface IUpcomingSpecification<TEntity> where TEntity : class, IHasDateRange
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query);

    IQueryable<TParent> ApplyExpression<TParent>(
        IQueryable<TParent> query,
        Expression<Func<TParent, TEntity>> navigation);
}
