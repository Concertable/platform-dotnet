using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    private readonly string schemaName;

    public OutboxMessageEntityConfiguration(IOptions<OutboxOptions> options)
    {
        schemaName = options.Value.SchemaName;
    }

    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable(Schema.Outbox, schemaName);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.MessageType).IsRequired().HasColumnType("nvarchar(450)");
        builder.Property(m => m.Payload).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(m => m.OccurredAtUtc).IsRequired();
        builder.Property(m => m.CorrelationId).HasColumnType("nvarchar(450)");
        builder.Property(m => m.Kind).HasConversion<int>().IsRequired();
        builder.Property(m => m.Status).HasConversion<int>().IsRequired();
        builder.Property(m => m.DispatchedAtUtc);
        builder.Property(m => m.Attempts).IsRequired();
        builder.Property(m => m.LastError);
        builder.HasIndex(m => new { m.Status, m.OccurredAtUtc });
    }
}
