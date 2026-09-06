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

### The pinned-image resource-graph assertions are copy-pasted into all four service startup suites

`AssertImageEndpoint`, `AssertContainerRuntimeArgs` and `AssertUsesDeveloperCertificate` are declared verbatim in the `ResourceGraphTests` of `Concertable.B2B.StartupTests`, `Concertable.Customer.StartupTests`, `Concertable.Payment.StartupTests` and `Concertable.Search.StartupTests` (about 45 lines each). Their natural home is `Concertable.Testing.Architecture`, which every one of those suites already references as a published package — so landing them is a publish-then-consume two-step. Moving them there means that package taking an `Aspire.Hosting` and `Concertable.AppHost.Shared` dependency — it currently has neither — so a shared-testing package would start carrying the AppHost graph vocabulary.

**Resolves when:** the three helpers exist once in `Concertable.Testing.Architecture` (or a new AppHost-graph testing package), all four suites call them from there, and no service startup suite declares its own copy.

---

## LOW

### `AppModelConfiguration` is triplicated byte-for-byte across three startup suites

`Concertable.Auth.StartupTests`, `Concertable.Payment.StartupTests` and `Concertable.Search.StartupTests` each carry a 49-line `AppModelConfiguration.cs` differing only in its namespace declaration. It is the piece that makes the tier's central gate work — it resolves what an AppHost resource is actually handed — so three copies means three places for the `Secrets` allowlist to drift, and a topology key wrongly added to one copy blinds only that service while the other two still look correct. Same destination and the same publish-then-consume constraint as the entry above.

**Resolves when:** `AppModelConfiguration` exists once in `Concertable.Testing.Architecture` (or the same new AppHost-graph testing package), every service startup suite resolves its app-model configuration through it, and no suite declares its own copy.

---

## LOW

### The startup gate's `IStartupValidator` call is inert wherever `ValidateOnStart` has not landed

`AppModelStartupContractTests` ends each case with `app.Services.GetService<IStartupValidator>()?.Validate()`. That service exists only once something has called `ValidateOnStart()`, and the only calls in `api/` are three in `Concertable.Payment.Infrastructure`. For Auth and Search the line therefore asserts nothing, and the gate's actual bite is the eager `?? throw` inside each host's `Configure` lambda firing during `builder.Build()`. The null-conditional keeps the tier forward-compatible as options validation lands, but it also means deleting a `ValidateOnStart()` weakens the gate without turning anything red.

**Resolves when:** every executable host declares its required configuration through `IValidateOptions<T>` plus `ValidateOnStart()`, and `AppModelStartupContractTests` resolves `IStartupValidator` with `GetRequiredService` so a host that stops declaring its requirements fails the tier instead of silently skipping the check.
