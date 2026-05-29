using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Messaging.Infrastructure.Inbox;

internal class InboxMessageEntityConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable(Schema.Inbox, Schema.Name);
        builder.HasKey(m => new { m.MessageId, m.ConsumerName });
        builder.Property(m => m.MessageId).ValueGeneratedNever();
        builder.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
        builder.Property(m => m.MessageType).IsRequired().HasColumnType("nvarchar(450)");
        builder.Property(m => m.ReceivedAt).IsRequired();
    }
}
