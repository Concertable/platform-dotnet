using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using MessagingSchema = Concertable.Messaging.Infrastructure.Schema;

namespace Concertable.DataAccess.Infrastructure;

public abstract class DbContextBase(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.ToTable(MessagingSchema.Tables.Outbox, MessagingSchema.Name, t => t.ExcludeFromMigrations());
            b.Property(m => m.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<InboxMessageEntity>(b =>
        {
            b.ToTable(MessagingSchema.Tables.Inbox, MessagingSchema.Name, t => t.ExcludeFromMigrations());
            b.HasKey(m => new { m.MessageId, m.ConsumerName });
            b.Property(m => m.MessageId).ValueGeneratedNever();
            b.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
            b.Property(m => m.MessageType).IsRequired().HasColumnType("nvarchar(450)");
            b.Property(m => m.ReceivedAt).IsRequired();
        });
    }

    public Task<bool> IsInboxMessageProcessedAsync(Guid messageId, string consumerName, CancellationToken ct = default)
        => Set<InboxMessageEntity>().AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName, ct);

    public void AddInboxMessage(MessageEnvelope envelope, string consumerName)
        => Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, consumerName, envelope.MessageType, DateTimeOffset.UtcNow));
}
