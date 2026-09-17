# Concertable.Messaging.IntegrationTests — integration tests

**Real PostgreSQL only.** The outbox claim is one PostgreSQL statement (`FOR UPDATE SKIP LOCKED`,
`UPDATE … FROM`, `RETURNING`), so no in-memory or SQLite provider can execute it. A messaging test that
does not need a database belongs in `Concertable.Messaging.UnitTests`.

Conventions: the `dotnet-standards:integration-testing` skill, plus `dotnet:unit-testing` for this
system's test-tier gate.
