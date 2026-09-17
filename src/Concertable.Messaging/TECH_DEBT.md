# Concertable.Messaging — technical debt

Debt local to the messaging libraries (`Concertable.Messaging.*` — Contracts, Application,
Infrastructure, AzureServiceBus). Cross-cutting messaging debt that also touches hosts or other
services (e.g. `AzureServiceBusOptions` binder defaults) lives in the root [`api/TECH_DEBT.md`](../TECH_DEBT.md).

---

## MED

### `AddAzureServiceBusTransport` runs the host `configure` lambda eagerly at registration

`Concertable.Messaging.AzureServiceBus/Extensions/ServiceCollectionExtensions.cs` probes `ServiceName` by constructing an `AzureServiceBusOptions` and calling `configure(options)` synchronously inside `AddAzureServiceBusTransport` (in addition to the deferred `services.Configure(configure)`). Because the host lambda runs at composition, any required-config throw inside it — notably `ConnectionString = GetConnectionString("asb") ?? throw` — fires at host **build**, not on real client resolution. That's invisible to DI mocking: an integration `Testing` host removes `AzureServiceBusReceiver` and swaps `IBusTransport`→`MockBusTransport`, yet still dies at boot because the throw lives in the app's own registration path. The workaround is a per-host `IsIntegration() ? null! : throw` guard on every asb site so test hosts boot (the `ServiceBusClient` is a lazy factory never resolved there, so `null!` is never read) — scattering test-awareness across the 7 host `Program.cs` files instead of a clean unconditional fail-fast.

**Resolves when:** the connection string is validated on resolution instead of by the eager probe (e.g. an `IValidateOptions<AzureServiceBusOptions>`, or a guard in the `ServiceBusClient` factory that runs when `IOptions.Value` is first read), so a missing asb faults only when the bus is actually used and hosts just bind the nullable `GetConnectionString("asb")`. The per-host `Testing` guards then drop. Rides a `Concertable.Messaging` package publish (alongside the `AzureServiceBusOptions = "" → null!` item in the root `TECH_DEBT.md`); keep the `ServiceName` eager fail-fast — it reads a literal that's always set.

**Progress — PR1 (package, `Chore/TechDebt`) landed the producer half:** `ConnectionString` is now validated in the `ServiceBusClient` singleton factory (fault on first resolution, not at registration), `ConnectionString` and `ServiceName` both flipped their `= ""` defaults to `null!` — required, assigned-before-use (this also closes the root `TECH_DEBT.md` `= "" → null!` item), and the eager `ServiceName` fail-fast is kept. Unit-covered in `AddAzureServiceBusTransportTests`. **Remaining — PR2 (delivery-gated on the republish + `platform-sync` pin bump, since all 7 hosts consume `Concertable.Messaging.AzureServiceBus` as a pinned `PackageReference`):** drop the `?? (IsIntegration() ? null! : throw …)` asb connection-string guard to `opts.ConnectionString = GetConnectionString("asb")!` (required; validated on resolution) in B2B.Web, B2B.Seed.Simulator, Customer.Web, Search.Workers, Payment.Web, Payment.Workers, and Auth — then delete this entry.

---

## LOW

### `OutboxOptions.SchemaName` is honoured at runtime but baked into the migration

`OutboxMessageEntityConfiguration`, `OutboxReader` and `DbContextBase` all resolve the outbox table's schema
from `IOptions<OutboxOptions>.SchemaName`, so the three runtime paths agree with each other. The
`InitialCreate` migration does not: it was scaffolded against the default and writes `EnsureSchema("messaging")`
and `schema: "messaging"` as literals. `OwnedSchemaSelector.TablesToIgnore` pins the two message-store
histories to `messaging` the same way, and `DbContextBase` still maps the **inbox** at the fixed
`Schema.Name` because `OutboxOptions` has no matching inbox option.

A host that sets `SchemaName` to anything else therefore reads and writes a table its migrations never
create, and its integration resets truncate a migration history the selector does not know to protect. The
option looks configurable and is not. EF compounds it by keying its model cache on the context *type*, so
the schema a `DbContextBase` subclass maps is fixed by whichever `OutboxOptions` built its model first —
per-instance variation silently does nothing.

**Resolves when:** either the option is honoured end to end — the migration and `OwnedSchemaSelector` derive
the schema from the same source, with a matching inbox option — or `SchemaName` is removed and the messaging
schema becomes the single `Schema.Name` constant. Both change a published contract every service consumes,
so it ships as its own package cutover.
