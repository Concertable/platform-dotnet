# Concertable.Shared — technical debt

Debt living in the shared platform tree (`Concertable.Kernel`, `Concertable.Shared.*`, the shared test
libs). Debt spanning multiple *services*, host `Program.cs` files, or repo-wide build/CI config belongs in
[`api/TECH_DEBT.md`](../TECH_DEBT.md); service-specific debt belongs in that service's own `TECH_DEBT.md`.

Everything here sits behind the published-package boundary: these libs are consumed cross-service by
`PackageReference` pinned to `$(ConcertablePlatformVersion)`, so a breaking change can't land atomically —
it needs a publish-first cut-over (see the `plans` skill, "Breaking published-contract changes"). That constraint is
why several items below are deferred rather than simply fixed.

---

## MED

### Shared test libraries are ProjectReferenced across the service-folder boundary (carve leak)

`Concertable.Testing`, `Concertable.Testing.Integration`, and the shared `Concertable.Testing.E2E` harness
live under `Concertable.Shared/tests/` — i.e. in the Shared "repo" — yet every consuming test project
reaches them by a `ProjectReference` that **escapes its own service folder**
(`api/Concertable.B2B/src/Modules/.../Tests/*.csproj → ..\..\..\..\..\..\Concertable.Shared\tests\Concertable.Testing\...`).
That is exactly the cross-folder escape the runtime carve forbids for service projects (the
`PackageReference, never a ProjectReference` guard in the service `.csproj`s). Runtime deps that live in
the Shared tree (Kernel, Messaging) publish + are pinned; the shared **test** libs alone leak straight
into every service's test projects. On a real repo split those references break. `Concertable.Testing`
even carries `IsPackable=true` with **zero** package consumers — a half-committed intent. First flagged
adding a shared `Money` test helper for the door-revenue UI E2E: it compiled same-PR *because* of this
leak, where a Kernel helper needs a publish-first PR.

**Resolves when:** the shared test libs are published as test-support packages consumed by pinned
`PackageReference` like the runtime shared libs (carrying the same publish-first + pin-bump boundary) —
OR test infra is explicitly documented as carve-exempt (dev-only, never shipped in a service runtime)
and the misleading `IsPackable=true` is dropped. Decision + execution steps:
[`plans/platform/SHARED_TEST_LIBS_PACKAGING.md`](../../plans/platform/SHARED_TEST_LIBS_PACKAGING.md). Lean: publish, for
consistency with the Shared-repo model — the cost is that every shared-test-helper edit then takes the
publish-first cycle.

### Outbox quiescence across an integration reset lives in B2B's fixture, not the shared library

`ApiFixture.ResetAsync` Respawns the database between tests while `OutboxDispatcher`, a `BackgroundService`
polling every second, may already hold a claimed batch it has not yet delivered — so the previous test's
messages land in the next test, after its mocks were cleared. B2B fixed this by stopping every live host's
background services before Respawn and starting them again after seeding, and by tracking the extra hosts
`CreateClient(user, configure)` builds so their dispatchers stop too.

That fix sits in `api/Concertable.B2B/tests/Concertable.B2B.IntegrationTests.Fixtures/ApiFixture.cs`. Customer,
Payment and Auth each register the same dispatcher behind their own `ApiFixture` and have the same hole open;
they are green today only because their suites generate less cross-reset outbox traffic.

It could not be lifted into `Concertable.Testing.Integration` in the same stroke: B2B consumes that library
as a pinned package and publishing runs only on `main`, so the shared change and its consumer cannot land
together. The same publish-first constraint blocks the tidier fix of having `OutboxDispatcher` and
`QueueHostedService` swallow cancellation in `ExecuteAsync` — both let it escape, which is why the B2B test
host has to set `BackgroundServiceExceptionBehavior.Ignore` to stop a cancelled loop tearing down the host.

**Resolves when:** the stop-before-Respawn / start-after-seed step is a member of the shared integration
testing library, B2B's fixture calls it instead of carrying its own copy, and the Customer, Payment and Auth
fixtures call it too.

---

### Seeding defects can only surface in E2E, because nothing cheaper runs a dev seeder

`IDevSeeder` runs in dev and E2E; `ITestSeeder` runs in integration. Two seeders write the same rows by
different code paths, so a fix can land on one path and leave the other broken with every gate still green.
That is not hypothetical — it is exactly what happened on
`Refactor/launch_deal-lifecycle-modules-phase2`.

`SeedingIdentityInterceptor` exists so no seeder hand-writes `SET IDENTITY_INSERT`. Its regex matched only
literal `INSERT INTO <table> (cols)`, and EF emits a `MERGE` for batched inserts into a TPH table — so the
interceptor silently did nothing for exactly the three modules with `HasDiscriminator` (Application, Booking,
Concert) and worked everywhere else. Someone hit the resulting error and pasted `SET IDENTITY_INSERT ON/OFF`
into `ApplicationTestSeeder`, `BookingTestSeeder` and `ConcertTestSeeder`. That fixed the integration tier and
masked the interceptor bug, leaving the dev seeders broken. It surfaced when E2E first ran on the branch: all
ten B2B API E2E tests failed on a health check, because `ApplicationDevSeeder` threw SQL 544 and `b2b-web`
exited. Roughly three hours to find, for a defect a millisecond-scale unit test would have caught.

The interceptor is fixed, the three workarounds are deleted, and its string logic now carries unit coverage:
`SeedingIdentityRewriter` holds the rewrite and the identity-table map, `Concertable.Seed.Shared.UnitTests`
covers both SQL shapes, a TPH model, a schema-qualified table, a database-generated identity and a
non-identity key, and the one-table-at-a-time constraint is enforced by a throw rather than recorded in a
`BookingFactory` comment. What remains, cheapest first:

2. **A dev-seeder smoke test per service in the integration tier.** Every `ApiFixture` already has
   containerized SQL and real migrations; it just runs `ITestSeeder`. One test that instead runs the
   `IDevSeeder` chain to completion catches identity mismatches, seeder ordering and FK violations in ~2
   minutes rather than ~30.
3. **An architecture test for seeder parity.** Every `ITestSeeder` has an `IDevSeeder` twin for the same
   module, and neither contains raw `IDENTITY_INSERT` SQL now the interceptor handles it. Unit-speed, and it
   would have failed the day the workaround was written instead of letting it mask a live bug.
4. **Collapse the dev/test seeder pair onto one implementation** differing only by interface, so a fix
   physically cannot land on one path. This defect required the divergence to exist.

E2E should be reserved for what genuinely needs real orchestration — cross-service wiring, Aspire, real HTTP.
Seeding needs a database and migrations, and the integration tier already has both.

**Resolves when:** items 2-3 are in place — each service's integration suite runs its `IDevSeeder` chain
against a migrated database, and an architecture test asserts dev/test seeder parity with no hand-written
`IDENTITY_INSERT` in either. Item 4 is the structural follow-up and may be tracked separately.

---

---

## LOW

### Calendar-boundary helpers are missing from Kernel

The Artist and Venue dashboard services each construct the UTC start of the current month with the
same `new DateTime(year, month, 1, ...)` expression. A generic `StartOfMonth()` operation belongs in
Kernel; implementing an identical extension independently inside each feature module would create two
owners for the same calendar rule.

**Resolves when:** Kernel exposes one C# 14 `DateTime.StartOfMonth()` extension that preserves the input
`DateTime.Kind`, the shared package is published, and all consumers migrate to it through the platform
sync rather than adding module-local copies.

### Shared DI extension methods carry a redundant `Shared` prefix (`AddSharedPdf`, not `AddPdf`)

The `IServiceCollection` extensions that wire the shared platform packages —
`AddSharedPdf`/`AddSharedEmail`/`AddSharedBlob`/`AddSharedGeocoding`/`AddSharedImaging`
(`src/Concertable.Shared.*.Infrastructure`) and `AddSharedInfrastructure` (`src/Concertable.Kernel`) —
repeat `Shared` in the method name though each already lives in a `Concertable.Shared.X` package, so a
caller reads `Concertable.Shared.Pdf.Infrastructure` → `AddSharedPdf()`. The new `AddQrCode()`
(`Concertable.Shared.QrCode`) drops the prefix; the existing ones can't follow in a bare edit — each is
the **public API of a published package** consumed cross-service by `PackageReference` (Auth calls
`AddSharedPdf`/`Blob`/`Email`/`Geocoding`/`Imaging`; Auth + Payment call `AddSharedInfrastructure`), so a
rename is a breaking change that reds `platform-sync` and can't be atomic (consumers can't move until the
new version is on the feed).

**Resolves when:** a repo-wide sweep drops the `Shared` prefix from every shared DI extension as a
publish-first package cut-over (rename in the package, publish, migrate consumers in the sync PR) — done
as one consistency pass, not piecemeal, so the codebase never mixes `AddPdf` next to `AddSharedEmail`.

### `GenreController` puts an HTTP surface in a shared library

`Concertable.Shared/src/Concertable.Shared.Api/Controllers/GenreController.cs` is one of only three
`public` controllers in the repo (36 are `internal`), and it sits in a shared library. The
`module-structure` skill's layer table says the opposite: "**Modules only** - a shared library exposes no
HTTP."

So either the rule needs a stated exception for a shared reference-vocabulary endpoint, or the controller
belongs in a service. Tommy's call; raised during the guidance-docs review 2026-08-18.

Resolves when: the controller moves to an owning service, or the `module-structure` skill states the
exception and this entry is deleted.

### Kernel still ships FluentResults, and two package references it never uses

`Concertable.Kernel.csproj` references `FluentResults`, `Newtonsoft.Json` and `Dapper`. Every service
consumes Kernel by pinned `PackageReference`, so all three land in every service's closure.

Verified, not assumed:

- **`Newtonsoft.Json` and `Dapper` are used by no `.cs` file** anywhere under
  `Concertable.Shared/src/`. Two direct references buying nothing, one of them a serializer every
  consumer then inherits alongside `System.Text.Json`.
- **FluentResults survives in exactly two Kernel files**, both at the Kernel root:
  `ErrorExtensions.SelectMessages` (an `IEnumerable<IError>` extension with **no callers in the repo** —
  its only occurrence is its own definition) and `BadRequestException`'s `IEnumerable<IError>` overload.
  The current terminal is already on the repo's own `Concertable.Kernel.Errors.IError`
  (`ErrorHttpExtensions.ToProblemActionResult<TError>`), so these two are the legacy carrier, not the
  live one.
- **The guard that bans it does not reach them.** `TypedResultArchitectureTests
  .KernelFunctionalTypes_DoNotReferenceThirdPartyCarriers` lists `FluentResults` as prohibited but
  enumerates `Concertable.Kernel/Functional` only, and both survivors sit outside that folder.

**Resolves when:** `SelectMessages` is deleted, `BadRequestException`'s FluentResults overload is
retyped or removed with its callers, the three package references are dropped, and the arch guard is
widened from `Functional/` to the whole Kernel so the carrier cannot come back. Publish-first: the
overload removal is breaking, so it migrates through a platform sync.

## `Concertable.Payment.Hosting` is pinned at the platform version, not the split Payment version

`api/Concertable.Shared/Directory.Packages.props` pins `Concertable.Payment.Hosting` at
`$(ConcertablePlatformVersion)`, while B2B and Customer pin every Payment package at the separate
`$(ConcertablePaymentVersion)` (`0.1.0-alpha.0.1322`) because Payment's alpha heights are not monotonic with
the platform's. Inert today: the only consumer is `Concertable.AppHost.Shared.UnitTests`, and
`PlatformSourcePackages.targets` swaps every `tests/`-path and `*.AppHost` project's Payment/Hosting
package reference to the in-repo project, so nothing here resolves Payment from the feed.

Found by independent review during PR #633 (finding IR36).

**Resolves when:** this file carries the same `ConcertablePaymentVersion` block as
`api/Concertable.B2B/Directory.Packages.props` and the `Concertable.Payment.Hosting` entry points at it —
after confirming against the feed which Payment versions are actually published, since the whole reason the
split pin exists is that the platform height is not a valid Payment height.

---
