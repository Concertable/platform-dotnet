# Concertable.Testing.Integration

Shared integration-test infrastructure. This is a reusable library — treat it like one.

Integration-test conventions (fixtures, `Respawn`, `TestAuthHandler`, test-seeding) are imported here:

@../../../docs/INTEGRATION_CONVENTIONS.md

## What belongs here

Only add something here if it is used by **two or more microservices**, *or* it is a **service-agnostic
contract** that services implement with their own service-specific types (the contract is shared; the
implementations are not). Current consumers: B2B, Customer, Search.

- `SqlFixture` — Testcontainers MsSql + Respawn reset
- `TestAuthHandler` — injects `sub` / `role` / `email` claims via request headers
- `IResettable` — marker interface for mocks that flush state between tests
- `Mocks/MockBusTransport` — no-op `IBusTransport` (suppresses real ASB)
- `Mocks/MockEmailSender` / `IMockEmailSender` — captures sent emails, exposes `Sent` list
- `Mocks/MockGeocodingService` / `MockGeocodingServiceFail` — stub geocoding
- `Mocks/MockImageService` — stub image upload/replace/delete
- `IWebhookSimulator` — service-agnostic contract for driving an inbound payment webhook in-process
  (e.g. dispatching `PaymentSucceededEvent` to its handlers). The **implementations**
  (`MockWebhookSimulator` etc.) are service-specific and live in the owning service's fixture.

## What does NOT belong here

If something is only used by one microservice it goes in that service's own fixture library, not here.

| Type of thing | Where it lives |
|---|---|
| `ApiFixture` for a specific service | that service's `Concertable.<Service>.IntegrationTests.Fixtures` |
| Service-specific mocks (Stripe, webhook simulator *implementations*, notification, payment client) | that service's `...IntegrationTests.Fixtures/Mocks/` |
| Service-specific DB initializers / seeders | that service's `...IntegrationTests.Fixtures` |
| Test collection definitions | that service's `...IntegrationTests.Fixtures` |

## Layout of per-service fixture libraries

This shared project lives at `api/Concertable.Shared/tests/Concertable.Testing.Integration/`. Each microservice
owns its own fixture library, located under that service, which **references this shared project**:

```
api/Concertable.Shared/tests/Concertable.Testing.Integration/            ← shared (this project)
api/Concertable.B2B/tests/Concertable.B2B.IntegrationTests.Fixtures/             ← B2B-only fixture, Stripe/webhook mocks
api/Concertable.Customer/tests/Concertable.Customer.IntegrationTests.Fixtures/   ← Customer-only fixture (owns MockCustomerPaymentClient)
api/Concertable.Search/tests/Concertable.Search.IntegrationTests.Fixtures/       ← Search-only fixture
```

Each service-owned fixture uses its own root namespace (`Concertable.<Service>.IntegrationTests.Fixtures`)
and references this shared project for `SqlFixture` / `TestAuthHandler` / `IResettable` / shared mocks.
