# Concertable.Shared — technical debt

Debt living in the shared platform tree (`Concertable.Kernel`, `Concertable.Shared.*`, the shared test
libs). Debt spanning multiple *services*, host `Program.cs` files, or repo-wide build/CI config belongs in
[`api/TECH_DEBT.md`](../TECH_DEBT.md); service-specific debt belongs in that service's own `TECH_DEBT.md`.

Everything here sits behind the published-package boundary: these libs are consumed cross-service by
`PackageReference` pinned to `$(ConcertablePlatformVersion)`, so a breaking change can't land atomically —
it needs a publish-first cut-over (see [`plans/agents/PLAN.md`](../../plans/agents/PLAN.md), "Boundary-blocked refactors"). That constraint is
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
