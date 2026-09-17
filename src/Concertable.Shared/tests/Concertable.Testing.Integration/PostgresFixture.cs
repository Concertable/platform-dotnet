using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace Concertable.Testing.Integration;

public sealed class PostgresFixture : IAsyncLifetime
{
    private const string PostgisImage = "postgis/postgis:17-3.5";

    private PostgreSqlContainer? container;
    private NpgsqlConnection? dbConnection;
    private Respawner? respawner;

    public string ConnectionString => (container ?? throw NotStarted()).GetConnectionString();

    private NpgsqlConnection Connection => dbConnection ?? throw NotStarted();

    private Respawner Respawner =>
        respawner ?? throw new InvalidOperationException(
            $"{nameof(InitializeRespawnerAsync)} has to run before {nameof(ResetAsync)}, and it has to run "
            + "after the migrations, so the schemas it is given are in the catalog.");

    public async Task InitializeAsync()
    {
        container = new PostgreSqlBuilder().WithImage(PostgisImage).Build();
        await container.StartAsync();
        dbConnection = new NpgsqlConnection(ConnectionString);
        await dbConnection.OpenAsync();
    }

    public async Task InitializeRespawnerAsync(IReadOnlyCollection<string> schemas)
    {
        var owned = OwnedSchemaSelector.Select(schemas, await ReadSchemaCatalogAsync());

        respawner = await Respawner.CreateAsync(Connection, new RespawnerOptions
        {
            SchemasToInclude = [.. owned],
            TablesToIgnore = [.. OwnedSchemaSelector.TablesToIgnore(owned)],
            DbAdapter = DbAdapter.Postgres,
            WithReseed = true
        });
    }

    public async Task ResetAsync() => await Respawner.ResetAsync(Connection);

    public async Task DisposeAsync()
    {
        if (dbConnection is not null)
            await dbConnection.DisposeAsync();
        if (container is not null)
            await container.DisposeAsync();
    }

    private async Task<IReadOnlyList<string>> ReadSchemaCatalogAsync()
    {
        await using var command = Connection.CreateCommand();
        command.CommandText = "SELECT nspname FROM pg_catalog.pg_namespace";

        await using var reader = await command.ExecuteReaderAsync();
        List<string> catalog = [];
        while (await reader.ReadAsync())
            catalog.Add(reader.GetString(0));

        return catalog;
    }

    private static InvalidOperationException NotStarted() =>
        new($"{nameof(InitializeAsync)} has to run before {nameof(InitializeRespawnerAsync)} and "
            + $"{nameof(ResetAsync)}.");
}
