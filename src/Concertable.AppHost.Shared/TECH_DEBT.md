# Concertable.AppHost.Shared — Technical Debt

When an item is fixed, update both this file and the root [`ARCHITECTURE.md`](../ARCHITECTURE.md).

---

## MEDIUM

### `AsbTopology.Subscribe`'s topic-name AND consumer-group strings can silently desync from the runtime

Two independent hand-typed string literals in every `X Topology.cs` (`PaymentTopology`, `B2BTopology`,
`CustomerTopology`, `SearchTopology`) each have a runtime counterpart they must match exactly, with
zero compile-time link to either:

1. **Topic name.** `AzureServiceBusOptions.TopicNameFor(Type eventType)`
   (`Concertable.Messaging.AzureServiceBus`) derives the actual ASB topic name at runtime as
   `EventTopicPrefix + eventType.Name.ToLowerInvariant()` — nobody hand-types it when calling
   `reg.Publishes<TEvent>()` / `reg.SubscribeTo<TEvent>()`. But `AsbTopology.Subscribe(string topic,
   ...)` takes the topic as a **hand-typed string literal**. Add/rename an event and forget (or typo)
   the matching `X Topology.cs` line and the mismatch surfaces only at runtime, deep into an E2E boot,
   as `Azure.Messaging.ServiceBus.ServiceBusException: MessagingEntityNotFound` — this is exactly what
   happened adding `PayoutOwnerRegisteredEvent` (see the now-deleted Payment `TECH_DEBT.md` entry it
   resolved) and cost real debugging time before the missing `PaymentTopology.cs` line was found.

2. **Consumer group.** The `consumerGroup` argument is the *actual* ASB subscription entity name (per
   Aspire's `AddServiceBusSubscription(name, subscriptionName)` — `subscriptionName` is the real Azure
   resource; the `subscription` argument `AsbTopology.Subscribe` also takes is just a cosmetic
   Aspire-graph label with no other reader). Within one `X Topology.cs` the same `consumerGroup` literal
   (e.g. `"concertable-payment"`) is repeated on every `.Subscribe()` line — but it is **also**
   independently hand-typed as each service's own `opts.ServiceName` in that service's own `Program.cs`
   (e.g. `Concertable.Payment.Web/Program.cs:67` and `Concertable.Payment.Workers/Program.cs:41` both
   write `"concertable-payment"` separately from `PaymentTopology.cs`'s five copies). Same failure mode
   as (1) if any of these three-plus independently-typed copies ever drifts.

**Resolves when:** `AsbTopology.Subscribe` takes the event `Type` (or is generic,
`Subscribe<TEvent>(string subscription)`) and calls `AzureServiceBusOptions.TopicNameFor` itself instead
of accepting a topic-name string — turning a missing/typo'd declaration into a compile error (unresolved
event type) instead of a runtime `MessagingEntityNotFound` — **and** each service's consumer-group name
becomes a single shared constant (e.g. `PaymentMessaging.ServiceName` in `Concertable.Payment.Contracts`,
mirrored per service) referenced by both that service's `Program.cs` files and its `X Topology.cs`,
with an `AsbTopology.ForService(string consumerGroup)` scoped builder so each `X Topology.cs` states the
constant once instead of repeating the literal on every `.Subscribe()` line.
