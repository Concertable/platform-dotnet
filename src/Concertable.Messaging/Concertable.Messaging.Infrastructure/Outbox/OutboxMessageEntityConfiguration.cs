using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    private readonly string schemaName;

    public OutboxMessageEntityConfiguration(IOptions<OutboxOptions> options)
    {
        schemaName = options.Value.SchemaName;
    }

    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable(Schema.Tables.Outbox, schemaName);
        builder.MapOutboxMessage();
    }
}
