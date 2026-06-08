# Concertable.AppHost.Shared

**Class library of reusable Aspire helpers.** No entry point — referenced by every executable AppHost in the repo (the umbrella `Concertable.AppHost` and each per-service `Concertable.X.AppHost`).

Holds extension methods over `IDistributedApplicationBuilder` / `IResourceBuilder<>` that any AppHost might need: SQL Server container, Azure Service Bus emulator, Azure Storage emulator, generic SPA / mobile / dev-tunnel scaffolding, generic env/secret helpers, the `AsbTopology` core that per-service `AddXTopology()` methods extend.

## What it is, what it isn't

- **It is** the home for cross-AppHost infrastructure helpers.
- **It is not** an executable — it has no `Program.cs`. Run an AppHost project to actually do anything.
- **It is not** the home for per-service wiring. A helper here must work regardless of which service is composing it. Names, ports, client IDs, secret keys, and inter-service dependencies belong in each service's `Concertable.X.AppHost.Extensions` library — see [`TECH_DEBT.md`](../Concertable.AppHost/TECH_DEBT.md) for the current violation and the target shape.

## Related projects

- [`../Concertable.AppHost/`](../Concertable.AppHost/README.md) — executable umbrella host that consumes these helpers.
- `api/Concertable.X/Concertable.X.AppHost/` — per-service executable hosts that also consume these helpers.
- `api/Concertable.X/Concertable.X.AppHost.Extensions/` — per-service libraries that contribute service-specific wiring on top of what's here.
