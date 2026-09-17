namespace Concertable.Messaging.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; set; } = 100;
    public int MaxAttempts { get; set; } = 20;
    public string SchemaName { get; set; } = Schema.Name;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
}
