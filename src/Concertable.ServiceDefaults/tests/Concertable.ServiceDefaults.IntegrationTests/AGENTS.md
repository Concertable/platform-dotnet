# Concertable.ServiceDefaults.IntegrationTests — integration tests

Conventions: [INTEGRATION_CONVENTIONS.md](../../../agents/INTEGRATION_CONVENTIONS.md)

Boots the pipeline over HTTP (`TestHost`), so it is an integration test, not a unit test. The
service-fixture apparatus in the conventions (`ApiFixture`, `[Collection("Integration")]`,
Testcontainers/Respawn) does not apply here: `Concertable.ServiceDefaults` is shared host infrastructure
with no service `Program` and no database, so these tests boot a minimal purpose-built `WebApplication`
to exercise the shared middleware directly.

@../../../agents/INTEGRATION_CONVENTIONS.md
