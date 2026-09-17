using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.Testing.Integration.IntegrationTests;

[Collection(ResetScopeCollection.Name)]
public sealed class RelationalTestDatabaseExtensionsTests : IAsyncLifetime
{
    private const string Constraint = "CK_Sentinel_NameIsNotBlank";

    private readonly ResetScopeFixture fixture;
    private readonly SentinelDbContext context;

    public RelationalTestDatabaseExtensionsTests(ResetScopeFixture fixture)
    {
        this.fixture = fixture;
        this.context = new SentinelDbContext(
            new DbContextOptionsBuilder<SentinelDbContext>().UseNpgsql(fixture.ConnectionString).Options);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public async Task DisposeAsync()
    {
        await context.Database.DropCheckConstraintIfExistsAsync(ResetScopeFixture.OwnedSchema, SentinelDbContext.Table, Constraint);
        await context.DisposeAsync();
    }

    #region AddUnvalidatedCheckConstraintAsync

    [Fact]
    public async Task AddUnvalidatedCheckConstraintAsync_ARowThePredicateRejects_IsRefusedByTheDatabase()
    {
        await AddConstraintAsync();

        var failure = await Assert.ThrowsAsync<PostgresException>(() => InsertAsync(""));

        Assert.Equal(PostgresErrorCodes.CheckViolation, failure.SqlState);
    }

    [Fact]
    public async Task AddUnvalidatedCheckConstraintAsync_RowsThatAlreadyBreakThePredicate_AreLeftAlone()
    {
        await InsertAsync("");

        await AddConstraintAsync();

        Assert.Equal(1, await fixture.ScalarAsync(
            $"""SELECT count(*) FROM "{ResetScopeFixture.OwnedSchema}"."{SentinelDbContext.Table}" """));
    }

    #endregion

    #region DropCheckConstraintIfExistsAsync

    [Fact]
    public async Task DropCheckConstraintIfExistsAsync_AfterTheConstraintIsGone_AcceptsTheRowAgain()
    {
        await AddConstraintAsync();

        await context.Database.DropCheckConstraintIfExistsAsync(ResetScopeFixture.OwnedSchema, SentinelDbContext.Table, Constraint);

        await InsertAsync("");
    }

    [Fact]
    public async Task DropCheckConstraintIfExistsAsync_NoSuchConstraint_DoesNothing() =>
        await context.Database.DropCheckConstraintIfExistsAsync(ResetScopeFixture.OwnedSchema, SentinelDbContext.Table, Constraint);

    #endregion

    private Task AddConstraintAsync() =>
        context.Database.AddUnvalidatedCheckConstraintAsync(
            ResetScopeFixture.OwnedSchema, SentinelDbContext.Table, Constraint, """length("Name") > 0""");

    private Task InsertAsync(string name) =>
        context.Database.ExecuteSqlRawAsync(
            $"""INSERT INTO "{ResetScopeFixture.OwnedSchema}"."{SentinelDbContext.Table}" ("Name") VALUES ('{name}')""");
}

internal sealed class SentinelDbContext : DbContext
{
    public const string Table = "Sentinel";

    public SentinelDbContext(DbContextOptions<SentinelDbContext> options)
        : base(options) { }
}
