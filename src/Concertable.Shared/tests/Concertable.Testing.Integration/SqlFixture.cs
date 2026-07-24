using System.Data.Common;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;
using Xunit;

namespace Concertable.Testing.Integration;

public sealed class SqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder().Build();
    private DbConnection dbConnection = null!;
    private Respawner respawner = null!;

    public string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        dbConnection = new SqlConnection(ConnectionString);
        await dbConnection.OpenAsync();
    }

    public async Task InitializeRespawnerAsync()
    {
        respawner = await Respawner.CreateAsync(dbConnection, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"],
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true
        });
    }

    public async Task ResetAsync() => await respawner.ResetAsync(dbConnection);

    public async Task DisposeAsync()
    {
        await dbConnection.DisposeAsync();
        await container.DisposeAsync();
    }
}
