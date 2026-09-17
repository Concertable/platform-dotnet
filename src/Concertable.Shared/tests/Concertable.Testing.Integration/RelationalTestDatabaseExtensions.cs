using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.Testing.Integration;

public static class RelationalTestDatabaseExtensions
{
    extension(DatabaseFacade database)
    {
        public string DelimitIdentifier(string identifier) =>
            database.GetService<ISqlGenerationHelper>().DelimitIdentifier(identifier);

        public Task AddUnvalidatedCheckConstraintAsync(
            string schema,
            string table,
            string constraint,
            string predicate,
            CancellationToken cancellationToken = default) =>
            database.ExecuteSqlRawAsync(
                $"ALTER TABLE {database.DelimitIdentifier(table, schema)}\n"
                + $"ADD CONSTRAINT {database.DelimitIdentifier(constraint)} CHECK ({predicate}) NOT VALID;",
                cancellationToken);

        public Task DropCheckConstraintIfExistsAsync(
            string schema,
            string table,
            string constraint,
            CancellationToken cancellationToken = default) =>
            database.ExecuteSqlRawAsync(
                $"ALTER TABLE {database.DelimitIdentifier(table, schema)} "
                + $"DROP CONSTRAINT IF EXISTS {database.DelimitIdentifier(constraint)};",
                cancellationToken);

        private string DelimitIdentifier(string table, string schema) =>
            database.GetService<ISqlGenerationHelper>().DelimitIdentifier(table, schema);
    }
}
