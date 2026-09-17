using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.IntegrationTests;

public sealed class DataAccessFixture : IAsyncLifetime
{
    public const string Schema = "dataaccess";

    private readonly PostgresFixture postgres = new();

    public async Task InitializeAsync()
    {
        await postgres.InitializeAsync();

        await using (var context = CreateContext())
            await context.Database.EnsureCreatedAsync();

        await postgres.InitializeRespawnerAsync([Schema]);
    }

    public Task ResetAsync() => postgres.ResetAsync();

    public Task DisposeAsync() => postgres.DisposeAsync();

    public ReferenceDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReferenceDbContext>().UseNpgsql(postgres.ConnectionString).Options);
}

[CollectionDefinition(Name)]
public sealed class DataAccessCollection : ICollectionFixture<DataAccessFixture>
{
    public const string Name = "DataAccess";
}
