@{
    Environment = @{
        # The existing platform design-time factory reads this key; no service database is opened.
        ConnectionStrings__B2BDb = 'Host=localhost;Database=concertable-messaging-design;Username=design;Password=design'
    }
    Migrations = @(
        @{ Context = 'OutboxDbContext'; Project = 'Concertable.Messaging.Infrastructure'; StartupProject = 'Concertable.Messaging.Infrastructure'; OutputDir = 'Data/Migrations/Outbox' }
        @{ Context = 'InboxDbContext'; Project = 'Concertable.Messaging.Infrastructure'; StartupProject = 'Concertable.Messaging.Infrastructure'; OutputDir = 'Data/Migrations/Inbox' }
    )
}
