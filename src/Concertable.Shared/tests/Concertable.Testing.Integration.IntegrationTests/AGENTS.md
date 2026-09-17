# Concertable.Testing.Integration.IntegrationTests — integration tests

Proves `PostgresFixture` against a real PostgreSQL container: what a reset deletes, and what it has to
leave standing. A test that can answer its question from the selector's inputs alone belongs in
`Concertable.Testing.Integration.UnitTests`.

Conventions: the `dotnet-standards:integration-testing` skill, plus `dotnet:unit-testing` for this
system's test-tier gate.
