# Concertable.Messaging — technical debt

Debt local to the messaging libraries (`Concertable.Messaging.*` — Contracts, Application,
Infrastructure, AzureServiceBus). Cross-cutting messaging debt that also touches hosts or other
services (e.g. `AzureServiceBusOptions` binder defaults) lives in the root [`api/TECH_DEBT.md`](../TECH_DEBT.md).

---

## MED

### `AddAzureServiceBusTransport` runs the host `configure` lambda eagerly at registration

`Concertable.Messaging.AzureServiceBus/Extensions/ServiceCollectionExtensions.cs` probes `ServiceName` by constructing an `AzureServiceBusOptions` and calling `configure(options)` synchronously inside `AddAzureServiceBusTransport` (in addition to the deferred `services.Configure(configure)`). Because the host lambda runs at composition, any required-config throw inside it — notably `ConnectionString = GetConnectionString("asb") ?? throw` — fires at host **build**, not on real client resolution. That's invisible to DI mocking: an integration `Testing` host removes `AzureServiceBusReceiver` and swaps `IBusTransport`→`MockBusTransport`, yet still dies at boot because the throw lives in the app's own registration path. The workaround is a per-host `IsEnvironment("Testing") ? null! : throw` guard on every asb site so test hosts boot (the `ServiceBusClient` is a lazy factory never resolved there, so `null!` is never read) — scattering test-awareness across the 7 host `Program.cs` files instead of a clean unconditional fail-fast.

**Resolves when:** the connection string is validated on resolution instead of by the eager probe (e.g. an `IValidateOptions<AzureServiceBusOptions>`, or a guard in the `ServiceBusClient` factory that runs when `IOptions.Value` is first read), so a missing asb faults only when the bus is actually used and hosts just bind the nullable `GetConnectionString("asb")`. The per-host `Testing` guards then drop. Rides a `Concertable.Messaging` package publish (alongside the `AzureServiceBusOptions = "" → null!` item in the root `TECH_DEBT.md`); keep the `ServiceName` eager fail-fast — it reads a literal that's always set.
