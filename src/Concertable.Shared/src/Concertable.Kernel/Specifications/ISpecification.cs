namespace Concertable.Kernel.Specifications;

public interface ISpecification<TEntity> where TEntity : class
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query);
}

public interface ISpecification<TEntity, TParams> where TEntity : class
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, TParams @params);
}
