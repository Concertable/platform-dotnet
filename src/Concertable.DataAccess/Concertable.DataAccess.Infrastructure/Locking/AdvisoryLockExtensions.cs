using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.DataAccess.Infrastructure.Locking;

public static class AdvisoryLockExtensions
{
    extension(DatabaseFacade database)
    {
        public Task AcquireAdvisoryLockAsync<TLock>(TLock lockNamespace, int key, CancellationToken ct = default)
            where TLock : struct, Enum =>
            database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({Convert.ToInt32(lockNamespace)}, {key})", ct);

        public Task AcquireAdvisoryLockAsync<TLock>(TLock lockNamespace, Guid key, CancellationToken ct = default)
            where TLock : struct, Enum =>
            database.AcquireAdvisoryLockAsync(lockNamespace, key.GetHashCode(), ct);

        public Task AcquireSharedAdvisoryLockAsync<TLock>(TLock lockNamespace, int key, CancellationToken ct = default)
            where TLock : struct, Enum =>
            database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock_shared({Convert.ToInt32(lockNamespace)}, {key})", ct);

        public Task AcquireSharedAdvisoryLockAsync<TLock>(TLock lockNamespace, Guid key, CancellationToken ct = default)
            where TLock : struct, Enum =>
            database.AcquireSharedAdvisoryLockAsync(lockNamespace, key.GetHashCode(), ct);
    }
}
