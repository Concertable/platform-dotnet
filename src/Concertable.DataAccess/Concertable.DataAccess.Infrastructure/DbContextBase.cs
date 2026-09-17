using Concertable.DataAccess.Application;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MessagingSchema = Concertable.Messaging.Infrastructure.Schema;

namespace Concertable.DataAccess.Infrastructure;

public abstract class DbContextBase : DbContext, IDbContext
{
    private readonly string outboxSchema;

    protected DbContextBase(DbContextOptions options, IOptions<OutboxOptions> outboxOptions)
        : base(options)
    {
        this.outboxSchema = outboxOptions.Value.SchemaName;
    }

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => Set<TEntity>();

    async Task IWriteDbContext.AddAsync<TEntity>(TEntity entity, CancellationToken ct) =>
        await Set<TEntity>().AddAsync(entity, ct);

    Task IWriteDbContext.AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken ct) =>
        Set<TEntity>().AddRangeAsync(entities, ct);

    void IWriteDbContext.Update<TEntity>(TEntity entity) => Set<TEntity>().Update(entity);

    void IWriteDbContext.Remove<TEntity>(TEntity entity) => Set<TEntity>().Remove(entity);

    Task<int> IWriteDbContext.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.ToTable(MessagingSchema.Tables.Outbox, outboxSchema, t => t.ExcludeFromMigrations());
            b.MapOutboxMessage();
        });

        modelBuilder.Entity<InboxMessageEntity>(b =>
        {
            b.ToTable(MessagingSchema.Tables.Inbox, MessagingSchema.Name, t => t.ExcludeFromMigrations());
            b.MapInboxMessage();
        });
    }

    public Task<bool> IsInboxMessageProcessedAsync(Guid messageId, string consumerName, CancellationToken ct = default)
        => Set<InboxMessageEntity>().AnyAsync(m => m.MessageId == messageId && m.ConsumerName == consumerName, ct);

    public void AddInboxMessage(MessageEnvelope envelope, string consumerName)
        => Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, consumerName, envelope.MessageType, DateTimeOffset.UtcNow));
}
