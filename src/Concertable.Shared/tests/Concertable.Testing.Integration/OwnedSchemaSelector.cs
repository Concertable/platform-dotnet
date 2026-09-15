using System.Collections.Frozen;
using Respawn.Graph;
using MessagingSchema = Concertable.Messaging.Infrastructure.Schema;

namespace Concertable.Testing.Integration;

/// <summary>
/// Decides what a reset covers: the schemas a service says it owns, checked against the catalog so a
/// system schema or a typo fails before anything is deleted, and the tables inside them a reset has to
/// leave standing — every migration history, and the tables an extension rather than a migration created.
/// </summary>
public static class OwnedSchemaSelector
{
    public const string MigrationsHistory = "__EFMigrationsHistory";

    private const string InboxMigrationsHistory = $"{MigrationsHistory}_Inbox";
    private const string OutboxMigrationsHistory = $"{MigrationsHistory}_Outbox";
    private const string PostgisSchema = "public";
    private const string PostgisReferenceSystems = "spatial_ref_sys";

    private static readonly FrozenSet<string> SqlServerSystemSchemas = new[]
    {
        "sys", "INFORMATION_SCHEMA", "guest", "db_owner", "db_accessadmin", "db_securityadmin",
        "db_ddladmin", "db_backupoperator", "db_datareader", "db_datawriter", "db_denydatareader",
        "db_denydatawriter"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Select(
        DatabaseProvider provider,
        IReadOnlyCollection<string> requested,
        IReadOnlyCollection<string> catalog)
    {
        if (requested.Count == 0)
            throw new InvalidOperationException(
                $"A {provider} reset needs the schemas this service owns, and none were named. Pass every schema "
                + $"its own migrations create, plus '{MessagingSchema.Name}' when it hosts a shared message store.");

        foreach (var schema in requested)
        {
            if (string.IsNullOrWhiteSpace(schema))
                throw new InvalidOperationException("A reset was given a blank schema name.");

            if (IsSystemSchema(provider, schema))
                throw new InvalidOperationException(
                    $"'{schema}' is a {provider} system schema, so no service owns it. Name only the schemas this "
                    + "service's own migrations create.");

            if (!catalog.Contains(schema, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    $"This database has no schema '{schema}'. It has: {string.Join(", ", catalog.Order(StringComparer.Ordinal))}. "
                    + "Migrate before initializing the respawner, and spell each schema the way the catalog does.");
        }

        return [.. requested];
    }

    public static IReadOnlyList<Table> TablesToIgnore(
        DatabaseProvider provider,
        IReadOnlyCollection<string> owned)
    {
        List<Table> tables =
        [
            .. owned.Select(schema => new Table(schema, MigrationsHistory)),
            new Table(MessagingSchema.Name, InboxMigrationsHistory),
            new Table(MessagingSchema.Name, OutboxMigrationsHistory)
        ];

        if (provider is DatabaseProvider.Postgres)
            tables.Add(new Table(PostgisSchema, PostgisReferenceSystems));

        return tables;
    }

    private static bool IsSystemSchema(DatabaseProvider provider, string schema) => provider switch
    {
        DatabaseProvider.SqlServer => SqlServerSystemSchemas.Contains(schema),
        DatabaseProvider.Postgres =>
            schema.StartsWith("pg_", StringComparison.OrdinalIgnoreCase)
            || schema.Equals("information_schema", StringComparison.OrdinalIgnoreCase),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };
}
