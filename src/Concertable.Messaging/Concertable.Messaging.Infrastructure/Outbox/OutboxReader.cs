using System.Data;
using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.Messaging.Infrastructure.Outbox;

internal sealed class OutboxReader : IOutboxReader
{
    private readonly OutboxDbContext context;
    private readonly OutboxOptions options;
    private readonly TimeProvider timeProvider;

    public OutboxReader(OutboxDbContext context, IOptions<OutboxOptions> options, TimeProvider timeProvider)
    {
        this.context = context;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<OutboxMessageEntity>> GetPendingAsync(int batchSize, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();

        if (!context.Database.IsSqlServer())
            return await context.Set<OutboxMessageEntity>()
                .Where(m => m.Status == OutboxStatus.Pending
                         && (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
                .OrderBy(m => m.OccurredAtUtc)
                .ThenBy(m => m.Id)
                .Take(batchSize)
                .ToListAsync(ct);

        var leaseExpiry = now.Add(options.LeaseDuration);
        var sql = $"""
            SET NOCOUNT ON;
            WITH claimed AS (
                SELECT TOP (@batchSize) *
                FROM [{options.SchemaName}].[{Schema.Tables.Outbox}] WITH (READPAST, UPDLOCK, ROWLOCK)
                WHERE (Status = @pending AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @now))
                   OR (Status = @dispatching AND NextRetryAtUtc <= @now)
                ORDER BY OccurredAtUtc, Id
            )
            UPDATE claimed
            SET Status = @dispatching, NextRetryAtUtc = @leaseExpiry
            OUTPUT inserted.Id, inserted.MessageType, inserted.Payload, inserted.OccurredAtUtc,
                   inserted.CorrelationId, inserted.Kind, inserted.Status, inserted.DispatchedAtUtc,
                   inserted.Attempts, inserted.LastError, inserted.NextRetryAtUtc;
            """;

        return await context.Set<OutboxMessageEntity>()
            .FromSqlRaw(sql,
                new SqlParameter("@batchSize", batchSize),
                new SqlParameter("@pending", SqlDbType.Int) { Value = (int)OutboxStatus.Pending },
                new SqlParameter("@dispatching", SqlDbType.Int) { Value = (int)OutboxStatus.Dispatching },
                new SqlParameter("@now", now),
                new SqlParameter("@leaseExpiry", leaseExpiry))
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
