namespace Concertable.Shared;

public interface ISearchSpecification<TEntity>
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, string? searchTerm);
}
