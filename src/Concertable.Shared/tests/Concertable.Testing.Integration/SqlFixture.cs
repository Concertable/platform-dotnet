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
    private DbConnection dbConnection = null!;
    private Respawner respawner = null!;

    protected virtual DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public DatabaseProvider ActiveProvider => resolvedProvider ??= ResolveProvider();

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        container = CreateContainer(ActiveProvider);
        await container.StartAsync();
        dbConnection = CreateConnection(ActiveProvider, ConnectionString);
        await dbConnection.OpenAsync();
    }

    public async Task InitializeRespawnerAsync() =>
        respawner = await Respawner.CreateAsync(dbConnection, CreateRespawnerOptions(ActiveProvider));

    public async Task ResetAsync() => await respawner.ResetAsync(dbConnection);

    public async Task DisposeAsync()
    {
        await dbConnection.DisposeAsync();
        await container.DisposeAsync();
    }

    private DatabaseProvider ResolveProvider() =>
        DatabaseProviderSelector.Resolve(
            Provider,
            Environment.GetEnvironmentVariable(DatabaseProviderSelector.ProviderVariable));

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

    private static RespawnerOptions CreateRespawnerOptions(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"],
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true
        },
        DatabaseProvider.Postgres => new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"],
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres,
            WithReseed = true
        },
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
    };

    // PostGIS ships as its own image rather than an extension of the stock postgres one, and eight
    // geography columns need it present from the first migration.
    private const string PostgisImage = "postgis/postgis:17-3.5";
}
