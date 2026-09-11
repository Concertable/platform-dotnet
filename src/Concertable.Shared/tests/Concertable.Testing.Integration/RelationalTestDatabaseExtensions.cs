using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Concertable.Testing.Integration;

public static class RelationalTestDatabaseExtensions
{
    private const string PostgreSqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private const string SqlServerProviderName = "Microsoft.EntityFrameworkCore.SqlServer";

    extension(DatabaseFacade database)
    {
        public string DelimitIdentifier(string identifier) =>
            database.GetService<ISqlGenerationHelper>().DelimitIdentifier(identifier);

        public Task ExecuteWithIdentityInsertAsync(
            string schema,
            string table,
            string commandText,
            CancellationToken cancellationToken = default)
        {
            var tableIdentifier = database.DelimitIdentifier(table, schema);
            var sql = database.ProviderName switch
            {
                SqlServerProviderName =>
                    $"SET IDENTITY_INSERT {tableIdentifier} ON;\n{commandText}\nSET IDENTITY_INSERT {tableIdentifier} OFF;",
                PostgreSqlProviderName => commandText,
                _ => throw UnsupportedProvider(database.ProviderName)
            };

            return database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        public Task AddUnvalidatedCheckConstraintAsync(
            string schema,
            string table,
            string constraint,
            string predicate,
            CancellationToken cancellationToken = default)
        {
            var tableIdentifier = database.DelimitIdentifier(table, schema);
            var constraintIdentifier = database.DelimitIdentifier(constraint);
            var sql = database.ProviderName switch
            {
                SqlServerProviderName =>
                    $"ALTER TABLE {tableIdentifier} WITH NOCHECK\nADD CONSTRAINT {constraintIdentifier} CHECK ({predicate});",
                PostgreSqlProviderName =>
                    $"ALTER TABLE {tableIdentifier}\nADD CONSTRAINT {constraintIdentifier} CHECK ({predicate}) NOT VALID;",
                _ => throw UnsupportedProvider(database.ProviderName)
            };

            return database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        public Task DropCheckConstraintIfExistsAsync(
            string schema,
            string table,
            string constraint,
            CancellationToken cancellationToken = default)
        {
            if (database.ProviderName is not (SqlServerProviderName or PostgreSqlProviderName))
                throw UnsupportedProvider(database.ProviderName);

            var tableIdentifier = database.DelimitIdentifier(table, schema);
            var constraintIdentifier = database.DelimitIdentifier(constraint);
            return database.ExecuteSqlRawAsync(
                $"ALTER TABLE {tableIdentifier} DROP CONSTRAINT IF EXISTS {constraintIdentifier};",
                cancellationToken);
        }

        private string DelimitIdentifier(string table, string schema) =>
            database.GetService<ISqlGenerationHelper>().DelimitIdentifier(table, schema);
    }

    private static NotSupportedException UnsupportedProvider(string? providerName) =>
        new($"The database provider '{providerName}' is not supported by the integration-test SQL helpers.");
}
