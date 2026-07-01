using System.Linq.Expressions;

namespace Concertable.Kernel.Specifications;

public interface INavigableSpecification<TEntity> : ISpecification<TEntity>
    where TEntity : class
{
    IQueryable<TNav> ApplyVia<TNav>(
        IQueryable<TNav> query,
        Expression<Func<TNav, TEntity>> navigation);
}

public interface INavigableSpecification<TEntity, TParams> : ISpecification<TEntity, TParams>
    where TEntity : class
{
    IQueryable<TNav> ApplyVia<TNav>(
        IQueryable<TNav> query,
        Expression<Func<TNav, TEntity>> navigation,
        TParams @params);
}
