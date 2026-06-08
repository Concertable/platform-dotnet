namespace Concertable.Messaging.Infrastructure;

public static class Schema
{
    public const string Name = "messaging";

    public static class Tables
    {
        public const string Inbox = "Inbox";
        public const string Outbox = "Outbox";
    }
}
