using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.Infrastructure.Extensions;

public static class WriteRepositoryExtensions
{
    extension<TEntity>(IWriteRepository<TEntity> repository) where TEntity : class
    {
        /// <summary>Add + save, returning <c>false</c> instead of throwing when the save fails on a
        /// duplicate key — for a caller that expects the conflict as a routine outcome rather than an
        /// exceptional one. Any other failure still propagates.</summary>
        public async Task<bool> TryInsertAsync(TEntity entity, CancellationToken ct = default)
        {
            await repository.AddAsync(entity, ct);

            try
            {
                await repository.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKey())
            {
                ex.DiscardFailedChanges();
                return false;
            }
        }
    }
}
