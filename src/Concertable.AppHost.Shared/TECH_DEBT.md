# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## LOW

### `AsbTopology.Subscribe<TEvent>(serviceName)` / `Queue<TCommand>(serviceName)` are superseded but still the live call sites

`AsbTopology` now also exposes `WithService(serviceName)`, scoping subsequent `Subscribe<TEvent>()`/`Queue<TCommand>()` calls to one service identity instead of repeating it per registration. The five service topologies (`AddAuthTopology`, `AddB2BTopology`, `AddCustomerTopology`, `AddPaymentTopology`, `AddSearchTopology`) still call the older `Subscribe<TEvent>(serviceName)`/`Queue<TCommand>(serviceName)` overloads, because they consume `Concertable.AppHost.Shared` as a published package pinned per service — migrating them can only land once that package has republished with `WithService` and each service's pin has bumped.

**Resolves when:** every `AddXTopology()` method is migrated to `.WithService(XConstants.ServiceName).Subscribe<...>()...Queue<...>()`, and the now-unused `Subscribe<TEvent>(string)`/`Queue<TCommand>(string)` overloads are deleted from `AsbTopology`.
