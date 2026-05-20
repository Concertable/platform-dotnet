namespace Concertable.Messaging.Domain;

public enum OutboxStatus
{
    Pending = 0,
    Dispatched = 1,
    DeadLettered = 2,
}
