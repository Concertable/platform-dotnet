namespace Concertable.DataAccess.Application;

public interface IReadDbContext
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : class;
}
