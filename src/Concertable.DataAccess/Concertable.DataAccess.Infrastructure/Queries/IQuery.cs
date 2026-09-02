namespace Concertable.DataAccess.Infrastructure.Queries;

internal interface IQuery<TEntity, in TParams> where TEntity : class
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, TParams @params);
}

internal interface IQuery<TSource, in TParams, TResult> where TSource : class
{
    IQueryable<TResult> Apply(IQueryable<TSource> query, TParams @params);
}
