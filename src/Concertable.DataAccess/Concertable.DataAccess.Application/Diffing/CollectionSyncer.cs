using Concertable.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.DataAccess.Application.Diffing;

public abstract class CollectionSyncer<TEntity, TDto>
    : ICollectionSyncer<TEntity, TDto>
    where TEntity : class, IIdEntity
    where TDto : ISyncRequest
{
    private readonly IWriteRepository<TEntity> repository;

    protected CollectionSyncer(IWriteRepository<TEntity> repository) => this.repository = repository;

    public async Task SyncAsync(
        int ownerId,
        IEnumerable<TEntity> current,
        IEnumerable<TDto> desired)
    {
        var byId = desired
            .Where(d => d.Id.HasValue)
            .ToDictionary(d => d.Id!.Value);

        var matched = current
            .Where(e => byId.ContainsKey(e.Id))
            .ToHashSet();

        foreach (var entity in matched)
            await UpdateAsync(entity, byId[entity.Id]);

        var creates = new List<TEntity>();
        foreach (var dto in desired.Where(d => !d.Id.HasValue))
            creates.Add(await CreateAsync(ownerId, dto));
        await repository.AddRangeAsync(creates);

        foreach (var entity in current.Except(matched))
            await DeleteAsync(entity);

        await repository.SaveChangesAsync();
    }

    protected abstract Task<TEntity> CreateAsync(int ownerId, TDto dto);
    protected abstract Task UpdateAsync(TEntity entity, TDto dto);

    protected virtual Task DeleteAsync(TEntity entity)
    {
        repository.Remove(entity);
        return Task.CompletedTask;
    }
}
