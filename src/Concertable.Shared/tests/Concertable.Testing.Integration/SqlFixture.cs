using System.Data.Common;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Npgsql;
using Respawn;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Concertable.Testing.Integration;

public class SqlFixture : IAsyncLifetime
{
    private DatabaseProvider? resolvedProvider;
    private IDatabaseContainer container = null!;
    private DbConnection? dbConnection;
    private Respawner respawner = null!;

    protected virtual DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public DatabaseProvider ActiveProvider => resolvedProvider ??= ResolveProvider();

    public string ConnectionString => container.GetConnectionString();

    private DbConnection Connection =>
        dbConnection ?? throw new InvalidOperationException(
            $"{nameof(InitializeAsync)} has to run before {nameof(InitializeRespawnerAsync)} and "
            + $"{nameof(ResetAsync)}.");

    public async Task InitializeAsync()
    {
        container = CreateContainer(ActiveProvider);
        await container.StartAsync();
        dbConnection = CreateConnection(ActiveProvider, ConnectionString);
        await dbConnection.OpenAsync();
    }

    public async Task InitializeRespawnerAsync()
    {
        if (ActiveProvider is DatabaseProvider.Postgres)
            throw new InvalidOperationException(
                $"A {DatabaseProvider.Postgres} reset has to name the schemas this service owns: left to the whole "
                + "database it reaches the migration histories and the tables PostGIS installed, which it must "
                + $"leave standing. Call {nameof(InitializeRespawnerAsync)} with them instead.");

        respawner = await Respawner.CreateAsync(Connection, new RespawnerOptions
        {
            TablesToIgnore = [OwnedSchemaSelector.MigrationsHistory],
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true
        });
    }

    public async Task InitializeRespawnerAsync(IReadOnlyCollection<string> schemas)
    {
        var owned = OwnedSchemaSelector.Select(ActiveProvider, schemas, await ReadSchemaCatalogAsync());

        respawner = await Respawner.CreateAsync(Connection, new RespawnerOptions
        {
            SchemasToInclude = [.. owned],
            TablesToIgnore = [.. OwnedSchemaSelector.TablesToIgnore(ActiveProvider, owned)],
            DbAdapter = CreateAdapter(ActiveProvider),
            WithReseed = true
        });
    }

    public async Task ResetAsync() => await respawner.ResetAsync(Connection);

    public async Task DisposeAsync()
    {
        if (dbConnection is not null)
            await dbConnection.DisposeAsync();
        await container.DisposeAsync();
    }

    private DatabaseProvider ResolveProvider() =>
        DatabaseProviderResolver.Resolve(
            Provider,
            Environment.GetEnvironmentVariable(DatabaseProviderResolver.ProviderVariable));

    private async Task<IReadOnlyList<string>> ReadSchemaCatalogAsync()
    {
        await using var command = Connection.CreateCommand();
        command.CommandText = CatalogSchemaSql(ActiveProvider);

        await using var reader = await command.ExecuteReaderAsync();
        List<string> catalog = [];
        while (await reader.ReadAsync())
            catalog.Add(reader.GetString(0));

        return catalog;
    }

    private static IDatabaseContainer CreateContainer(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => new MsSqlBuilder().Build(),
        DatabaseProvider.Postgres => new PostgreSqlBuilder().WithImage(PostgisImage).Build(),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static DbConnection CreateConnection(DatabaseProvider provider, string connectionString) => provider switch
    {
        DatabaseProvider.SqlServer => new SqlConnection(connectionString),
        DatabaseProvider.Postgres => new NpgsqlConnection(connectionString),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static IDbAdapter CreateAdapter(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => DbAdapter.SqlServer,
        DatabaseProvider.Postgres => DbAdapter.Postgres,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    private static string CatalogSchemaSql(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => "SELECT name FROM sys.schemas",
        DatabaseProvider.Postgres => "SELECT nspname FROM pg_catalog.pg_namespace",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    // PostGIS is its own image, not an extension, and geography columns need it from migration one.
    private const string PostgisImage = "postgis/postgis:17-3.5";
}
