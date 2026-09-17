using System.Data.Common;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.DataAccess.IntegrationTests;

[Collection(DataAccessCollection.Name)]
public sealed class DuplicateKeyPersistenceTests : IAsyncLifetime
{
    private readonly DataAccessFixture fixture;

    public DuplicateKeyPersistenceTests(DataAccessFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region IsDuplicateKey

    [Fact]
    public async Task IsDuplicateKey_ASecondRowOnAUniqueIndex_IsTrue()
    {
        await InsertAsync("VENUE", "First");

        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => InsertAsync("VENUE", "Second"));

        Assert.True(failure.IsDuplicateKey());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(failure.InnerException).SqlState);
    }

    [Fact]
    public async Task IsDuplicateKey_ANotNullViolation_IsFalse()
    {
        await using var context = fixture.CreateContext();

        var failure = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
            $"""INSERT INTO "{DataAccessFixture.Schema}"."References" ("Code", "Name") VALUES ('VENUE', NULL)"""));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, failure.SqlState);
        Assert.False(((DbException)failure).IsDuplicateKey());
    }

    #endregion

    #region GetOrCreateAsync

    [Fact]
    public async Task GetOrCreateAsync_ACodeNothingHasClaimed_InsertsIt()
    {
        await using var context = fixture.CreateContext();

        var created = await context.References.GetOrCreateAsync(
            new ReferenceEntity { Code = "VENUE", Name = "Venue" },
            entity => entity.Code,
            entity => entity.Code == "VENUE");

        Assert.NotEqual(0, created.Id);
        Assert.Equal("Venue", created.Name);
    }

    [Fact]
    public async Task GetOrCreateAsync_ACodeAlreadyTaken_ReturnsTheRowThatWon()
    {
        await InsertAsync("VENUE", "First");

        await using var context = fixture.CreateContext();
        var resolved = await context.References.GetOrCreateAsync(
            new ReferenceEntity { Code = "VENUE", Name = "Second" },
            entity => entity.Code,
            entity => entity.Code == "VENUE");

        Assert.Equal("First", resolved.Name);
        Assert.Single(await context.References.Where(entity => entity.Code == "VENUE").ToListAsync());
    }

    [Fact]
    public async Task GetOrCreateAsync_InsideACallersTransaction_LeavesThatTransactionUsable()
    {
        await InsertAsync("VENUE", "First");

        await using var context = fixture.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await context.References.GetOrCreateAsync(
            new ReferenceEntity { Code = "VENUE", Name = "Second" },
            entity => entity.Code,
            entity => entity.Code == "VENUE");

        context.References.Add(new ReferenceEntity { Code = "ARTIST", Name = "Artist" });
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        await using var probe = fixture.CreateContext();
        Assert.Equal(2, await probe.References.CountAsync());
    }

    [Fact]
    public async Task GetOrCreateAsync_TwoCallersRacingForTheSameCode_LeaveOneRowAndAgreeOnIt()
    {
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();

        var resolved = await Task.WhenAll(
            first.References.GetOrCreateAsync(
                new ReferenceEntity { Code = "VENUE", Name = "First" },
                entity => entity.Code,
                entity => entity.Code == "VENUE"),
            second.References.GetOrCreateAsync(
                new ReferenceEntity { Code = "VENUE", Name = "Second" },
                entity => entity.Code,
                entity => entity.Code == "VENUE"));

        await using var probe = fixture.CreateContext();
        Assert.Single(await probe.References.ToListAsync());
        Assert.Equal(resolved[0].Id, resolved[1].Id);
    }

    #endregion

    private async Task InsertAsync(string code, string name)
    {
        await using var context = fixture.CreateContext();
        context.References.Add(new ReferenceEntity { Code = code, Name = name });
        await context.SaveChangesAsync();
    }
}
