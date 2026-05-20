namespace Concertable.Messaging.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; init; } = 100;
    public int MaxAttempts { get; init; } = 10;
    public string SchemaName { get; init; } = "messaging";
}
