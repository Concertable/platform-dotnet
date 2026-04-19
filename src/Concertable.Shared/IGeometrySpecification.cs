namespace Concertable.Shared;

public interface IGeometrySpecification<TEntity> where TEntity : class, IEntity, ILocatable<TEntity>
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, IGeoParams geoParams);
}
