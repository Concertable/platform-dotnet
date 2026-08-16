# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## LOW

### `AsbTopology` repeats the service identity for every subscription and queue

`Subscribe<TEvent>(serviceName)` and `Queue<TCommand>(serviceName)` require every service topology to repeat the same service name for each registration. The identity is necessary because it gives each service its own durable event subscription and names its command queues, but it is a property of the consuming service topology rather than an individual message registration. The current API adds noise across B2B, Customer, Payment, Search, and Auth, and permits one registration in a chain to accidentally use a different service identity.

**Resolves when:** `AsbTopology` can be scoped once to a service, with message registrations made through that scoped builder, for example `topology.ForService(B2BConstants.ServiceName).Subscribe<CredentialRegisteredEvent>().Queue<SomeCommand>()`. Preserve independent per-service Azure Service Bus subscriptions and the existing topic, subscription, and queue names. Add focused topology tests covering naming and isolation before migrating every service topology.
