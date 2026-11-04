using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.DataAccess.Infrastructure.Locking;

public static class AdvisoryLockExtensions
{
    extension(DatabaseFacade database)
    {
        public Task AcquireAdvisoryLockAsync(int lockNamespace, int key, CancellationToken ct = default) =>
            database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({lockNamespace}, {key})", ct);

        public Task AcquireAdvisoryLockAsync(int lockNamespace, Guid key, CancellationToken ct = default) =>
            database.AcquireAdvisoryLockAsync(lockNamespace, key.GetHashCode(), ct);

        public Task AcquireSharedAdvisoryLockAsync(int lockNamespace, int key, CancellationToken ct = default) =>
            database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock_shared({lockNamespace}, {key})", ct);

        public Task AcquireSharedAdvisoryLockAsync(int lockNamespace, Guid key, CancellationToken ct = default) =>
            database.AcquireSharedAdvisoryLockAsync(lockNamespace, key.GetHashCode(), ct);
    }
}
