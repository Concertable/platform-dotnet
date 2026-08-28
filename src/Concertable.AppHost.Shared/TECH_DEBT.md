# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## LOW

### `AsbTopology.Subscribe<TEvent>(serviceName)` / `Queue<TCommand>(serviceName)` are superseded but still the live call sites

`AsbTopology` now also exposes `ForService(serviceName)`, scoping subsequent `Subscribe<TEvent>()`/`Queue<TCommand>()` calls to one service identity instead of repeating it per registration. The five service topologies (`AddAuthTopology`, `AddB2BTopology`, `AddCustomerTopology`, `AddPaymentTopology`, `AddSearchTopology`) still call the older `Subscribe<TEvent>(serviceName)`/`Queue<TCommand>(serviceName)` overloads, because they consume `Concertable.AppHost.Shared` as a published package pinned per service — migrating them can only land once that package has republished with `ForService` and each service's pin has bumped.

**Resolves when:** every `AddXTopology()` method is migrated to `.ForService(XConstants.ServiceName).Subscribe<...>()...Queue<...>()`, and the now-unused `Subscribe<TEvent>(string)`/`Queue<TCommand>(string)` overloads are deleted from `AsbTopology`.
