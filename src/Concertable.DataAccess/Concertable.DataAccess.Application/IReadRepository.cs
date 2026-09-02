using Concertable.Kernel;
using Concertable.Kernel.Specifications;

namespace Concertable.DataAccess.Application;

public interface IReadRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<TEntity?> GetByIdAsync(TKey id, ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<TResult?> GetByIdAsync<TResult>(TKey id, ISpecification<TEntity, TResult> spec, CancellationToken ct = default)
        where TResult : class;
    Task<TResult?> GetByIdAsync<TResult>(TKey id, ISpecification<TEntity, TResult?> spec, CancellationToken ct = default)
        where TResult : struct;
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<IReadOnlyList<TResult>> GetAllAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken ct = default);
    Task<IPagination<TEntity>> GetPageAsync(ISpecification<TEntity> spec, IPageParams pageParams, CancellationToken ct = default);
    Task<IPagination<TResult>> GetPageAsync<TResult>(ISpecification<TEntity, TResult> spec, IPageParams pageParams, CancellationToken ct = default);
    bool Exists(TKey id);
}

public interface IReadRepository<TEntity> : IReadRepository<TEntity, int>
    where TEntity : class, IIdEntity;
