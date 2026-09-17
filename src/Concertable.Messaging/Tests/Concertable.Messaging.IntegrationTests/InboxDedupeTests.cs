using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Concertable.Messaging.IntegrationTests;

[Collection(MessageStoreCollection.Name)]
public sealed class InboxDedupeTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Received = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);
    private const string MessageTypeName = "concertable.messaging.fake-integration-event.v1";

    private readonly MessageStoreFixture fixture;
    private readonly Guid messageId = Guid.NewGuid();

    public InboxDedupeTests(MessageStoreFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region Identity

    [Fact]
    public async Task SaveChangesAsync_TheSameMessageForTheSameConsumerTwice_IsRejectedAsAUniqueViolation()
    {
        await RecordAsync("ConcertProjector");

        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => RecordAsync("ConcertProjector"));

        var postgres = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
    }

    [Fact]
    public async Task SaveChangesAsync_TheSameMessageForTwoConsumers_RecordsBoth()
    {
        await RecordAsync("ConcertProjector");
        await RecordAsync("SearchProjector");

        await using var probe = fixture.CreateInboxContext();
        Assert.Equal(2, await probe.Set<InboxMessageEntity>().CountAsync(m => m.MessageId == messageId));
    }

    [Fact]
    public async Task SaveChangesAsync_AnInstantCarryingAnOffset_IsStoredAtUtc()
    {
        await RecordAsync("ConcertProjector", new DateTimeOffset(2026, 5, 20, 13, 0, 0, TimeSpan.FromHours(1)));

        await using var probe = fixture.CreateInboxContext();
        var stored = Assert.Single(await probe.Set<InboxMessageEntity>().ToListAsync());
        Assert.Equal(TimeSpan.Zero, stored.ReceivedAt.Offset);
        Assert.Equal(Received.UtcDateTime, stored.ReceivedAt.UtcDateTime);
    }

    #endregion

    private async Task RecordAsync(string consumerName, DateTimeOffset? receivedAt = null)
    {
        await using var context = fixture.CreateInboxContext();
        context.Add(InboxMessageEntity.Create(messageId, consumerName, MessageTypeName, receivedAt ?? Received));
        await context.SaveChangesAsync();
    }
}
