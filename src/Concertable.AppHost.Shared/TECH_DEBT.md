# Concertable.AppHost.Shared — technical debt

Debt local to the reusable Aspire hosting and topology helpers.

---

## HIGH

### The standalone B2B and Customer AppHosts never supply `services:payment-web:https:0`, so three hosts cannot start

`Concertable.B2B.AppHost` and `Concertable.Customer.AppHost` declare the pinned Payment web container as

```csharp
.WithHttpEndpoint(targetPort: 8080, name: "https")
.WithHttpEndpoint(targetPort: 8080, name: "http")
```

Aspire keys service-discovery configuration by an endpoint's **`UriScheme`, not its name**, and
`WithHttpEndpoint` sets the scheme to `http` whatever the name says. Both endpoints therefore collapse into
`services:payment-web:http:0` and `services:payment-web:http:1`, and `services:payment-web:https:0` — the key
`Concertable.Payment.Client.AddPaymentClient` requires and throws on — is never produced. That breaks
**b2b-web**, **b2b-workers** and **customer-web** at startup under `dotnet run` on either standalone AppHost,
which `AGENTS.md` calls the canonical entry point.

Nothing catches it today because the E2E harness sets the key by hand in three places
(`Concertable.B2B.E2ETests/DistributedApplicationBuilderExtensions.cs:66,89` and the Customer sibling at
`:68`), so the only path that exercises these hosts supplies what the app model does not.

Do **not** "fix" this by switching the first declaration to `WithHttpsEndpoint`: the pinned image serves
plaintext on 8080 (same constraint as the Auth image, see `2aba5fc2c`), so that would make the key appear and
every gRPC call over it fail at runtime — the endpoint-name-versus-scheme lie that caused RT3's Auth TLS
failure, moved one layer along. The real options are (a) give `Concertable.Payment.Client` a scheme-agnostic
address key it owns, matching the existing `Services:B2BApiUrl` / `Services:CustomerApiUrl` convention —
a published-contract change, so accept-new-with-fallback, publish, then migrate; or (b) run Payment from
source in the standalone AppHosts as E2E now does for Auth, which gives up part of the RT3 image cut-over.

**Resolves when:** `Concertable.B2B.StartupTests` and `Concertable.Customer.StartupTests` each carry the
`AppModelStartupContractTests` their siblings already have — covering b2b-web, b2b-workers and customer-web —
and those tests pass without the E2E harness's manual `services__payment-web__https__0` overrides, which are
deleted in the same stroke.

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
