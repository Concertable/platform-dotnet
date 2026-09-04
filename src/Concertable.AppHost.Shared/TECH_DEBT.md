# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## LOW

### `AsbTopology.Subscribe<TEvent>(serviceName)` / `Queue<TCommand>(serviceName)` are superseded but still the live call sites

`AsbTopology` now also exposes `WithService(serviceName)`, scoping subsequent `Subscribe<TEvent>()`/`Queue<TCommand>()` calls to one service identity instead of repeating it per registration. The five service topologies (`AddAuthTopology`, `AddB2BTopology`, `AddCustomerTopology`, `AddPaymentTopology`, `AddSearchTopology`) still call the older `Subscribe<TEvent>(serviceName)`/`Queue<TCommand>(serviceName)` overloads, because they consume `Concertable.AppHost.Shared` as a published package pinned per service — migrating them can only land once that package has republished with `WithService` and each service's pin has bumped.

**Resolves when:** every `AddXTopology()` method is migrated to `.WithService(XConstants.ServiceName).Subscribe<...>()...Queue<...>()`, and the now-unused `Subscribe<TEvent>(string)`/`Queue<TCommand>(string)` overloads are deleted from `AsbTopology`.

---

## LOW

### The pinned-image resource-graph assertions are copy-pasted into all four service startup suites

`AssertImageEndpoint`, `AssertContainerRuntimeArgs` and `AssertUsesDeveloperCertificate` are declared verbatim in the `ResourceGraphTests` of `Concertable.B2B.StartupTests`, `Concertable.Customer.StartupTests`, `Concertable.Payment.StartupTests` and `Concertable.Search.StartupTests` (about 45 lines each). Their natural home is `Concertable.Testing.Architecture`, which every one of those suites already references as a published package — so landing them is a publish-then-consume two-step. Moving them there means that package taking an `Aspire.Hosting` and `Concertable.AppHost.Shared` dependency — it currently has neither — so a shared-testing package would start carrying the AppHost graph vocabulary.

**Resolves when:** the three helpers exist once in `Concertable.Testing.Architecture` (or a new AppHost-graph testing package), all four suites call them from there, and no service startup suite declares its own copy.
