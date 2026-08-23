using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.Testing.Unit;

public static class InMemoryDatabaseRootExtensions
{
    extension(InMemoryDatabaseRoot root)
    {
        /// <summary>Builds a `TContext` against this root's InMemory database, hiding the
        /// `DbContextOptionsBuilder`/`UseInMemoryDatabase` ceremony every call site otherwise repeats.</summary>
        public TContext CreateContext<TContext>(
            string databaseName,
            Func<DbContextOptions<TContext>, TContext> create,
            QueryTrackingBehavior trackingBehavior = QueryTrackingBehavior.TrackAll)
            where TContext : DbContext
        {
            var options = new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase(databaseName, root)
                .UseQueryTrackingBehavior(trackingBehavior)
                .Options;
            return create(options);
        }
    }
}
