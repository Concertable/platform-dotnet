using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Messaging.Infrastructure.Inbox;

internal sealed class InboxMessageEntityConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable(Schema.Tables.Inbox, Schema.Name);
        builder.MapInboxMessage();
    }
}
