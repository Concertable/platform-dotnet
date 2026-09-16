using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Messaging.Infrastructure;

public static class EntityTypeBuilderExtensions
{
    extension(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        public EntityTypeBuilder<InboxMessageEntity> MapInboxMessage()
        {
            builder.HasKey(m => new { m.MessageId, m.ConsumerName });
            builder.Property(m => m.MessageId).ValueGeneratedNever();
            builder.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
            builder.Property(m => m.MessageType).IsRequired().HasMaxLength(450);
            builder.Property(m => m.ReceivedAt).IsRequired();
            return builder;
        }
    }

    extension(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        public EntityTypeBuilder<OutboxMessageEntity> MapOutboxMessage()
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.MessageType).IsRequired().HasMaxLength(450);
            builder.Property(m => m.Payload).IsRequired();
            builder.Property(m => m.OccurredAtUtc).IsRequired();
            builder.Property(m => m.CorrelationId).HasMaxLength(450);
            builder.Property(m => m.Kind).HasConversion<int>().IsRequired();
            builder.Property(m => m.Status).HasConversion<int>().IsRequired();
            builder.Property(m => m.DispatchedAtUtc);
            builder.Property(m => m.Attempts).IsRequired();
            builder.Property(m => m.LastError);
            builder.HasIndex(m => new { m.Status, m.OccurredAtUtc });
            return builder;
        }
    }
}
