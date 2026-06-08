namespace Concertable.Messaging.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; init; } = 100;
    public int MaxAttempts { get; init; } = 20;
    public string SchemaName { get; init; } = Schema.Name;
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(5);
}
