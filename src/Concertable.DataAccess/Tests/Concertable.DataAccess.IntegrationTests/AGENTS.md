# Concertable.DataAccess.IntegrationTests — integration tests

Two kinds of test share this project:

- **model shape** — a `DbContextBase` consumer built over an in-memory SQLite connection, proving what the
  shared base maps and what it leaves to the message-store's own migrations;
- **real-provider behaviour** — `DataAccessFixture` starts one PostgreSQL container so duplicate-key
  classification and `GetOrCreateAsync` are proved against the error codes and transaction semantics they
  actually have, not against a provider that cannot produce them.

A test that needs neither belongs in `Concertable.DataAccess.UnitTests`.

Conventions: the `dotnet-standards:integration-testing` skill, plus `dotnet:unit-testing` for this system's
test-tier gate.
