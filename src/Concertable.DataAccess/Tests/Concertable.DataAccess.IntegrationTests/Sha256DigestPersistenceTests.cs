using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Concertable.DataAccess.IntegrationTests;

[Collection(DataAccessCollection.Name)]
public sealed class Sha256DigestPersistenceTests : IAsyncLifetime
{
    private readonly DataAccessFixture fixture;

    public Sha256DigestPersistenceTests(DataAccessFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region Sha256Digest

    [Fact]
    public async Task Sha256Digest_SavedAndReloaded_RoundTripsWithoutAnyConfiguration()
    {
        var fingerprint = Sha256Digest.Of("VENUE", 42);
        await using (var context = fixture.CreateContext())
        {
            context.References.Add(new ReferenceEntity { Code = "VENUE", Name = "Venue", Fingerprint = fingerprint });
            await context.SaveChangesAsync();
        }

        await using var probe = fixture.CreateContext();
        var reloaded = await probe.References.SingleAsync(entity => entity.Fingerprint == fingerprint);

        Assert.Equal(fingerprint, reloaded.Fingerprint);
    }

    [Fact]
    public void Sha256Digest_Convention_MapsAFixedLengthColumnOfTheDigestLength()
    {
        using var context = fixture.CreateContext();

        var property = context.Model.FindEntityType(typeof(ReferenceEntity))!.FindProperty(nameof(ReferenceEntity.Fingerprint))!;

        Assert.Equal(Sha256Digest.Length, property.GetMaxLength());
        Assert.True(property.IsFixedLength());
    }

    #endregion
}
