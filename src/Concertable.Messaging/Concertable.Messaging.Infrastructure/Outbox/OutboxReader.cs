using Concertable.Messaging.Application;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

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
        var leaseExpiry = now.Add(options.LeaseDuration);

        var sql = $"""
            UPDATE "{options.SchemaName}"."{Schema.Tables.Outbox}" AS o
            SET "Status" = @dispatching, "NextRetryAtUtc" = @leaseExpiry
            FROM (
                SELECT "Id"
                FROM "{options.SchemaName}"."{Schema.Tables.Outbox}"
                WHERE ("Status" = @pending AND ("NextRetryAtUtc" IS NULL OR "NextRetryAtUtc" <= @now))
                   OR ("Status" = @dispatching AND "NextRetryAtUtc" <= @now)
                ORDER BY "OccurredAtUtc", "Id"
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
            ) AS claimed
            WHERE o."Id" = claimed."Id"
            RETURNING o."Id", o."MessageType", o."Payload", o."OccurredAtUtc", o."CorrelationId",
                      o."Kind", o."Status", o."DispatchedAtUtc", o."Attempts", o."LastError",
                      o."NextRetryAtUtc";
            """;

        return await context.Set<OutboxMessageEntity>()
            .FromSqlRaw(sql,
                new NpgsqlParameter("batchSize", NpgsqlDbType.Integer) { Value = batchSize },
                new NpgsqlParameter("pending", NpgsqlDbType.Integer) { Value = (int)OutboxStatus.Pending },
                new NpgsqlParameter("dispatching", NpgsqlDbType.Integer) { Value = (int)OutboxStatus.Dispatching },
                new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now },
                new NpgsqlParameter("leaseExpiry", NpgsqlDbType.TimestampTz) { Value = leaseExpiry })
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
