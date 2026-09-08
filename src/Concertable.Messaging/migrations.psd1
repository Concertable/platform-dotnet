@{
    Environment = @{
        # The existing platform design-time factory reads this key; no service database is opened.
        ConnectionStrings__B2BDb = 'Server=localhost;Database=concertable-messaging-design;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'OutboxDbContext'; Project = 'Concertable.Messaging.Infrastructure'; StartupProject = 'Concertable.Messaging.Infrastructure'; OutputDir = 'Data/Migrations/Outbox' }
        @{ Context = 'InboxDbContext'; Project = 'Concertable.Messaging.Infrastructure'; StartupProject = 'Concertable.Messaging.Infrastructure'; OutputDir = 'Data/Migrations/Inbox' }
    )
}
