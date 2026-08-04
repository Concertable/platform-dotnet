# Concertable.Shared — technical debt

Debt living in the shared platform tree (`Concertable.Kernel`, `Concertable.Shared.*`, the shared test
libs). Debt spanning multiple *services*, host `Program.cs` files, or repo-wide build/CI config belongs in
[`api/TECH_DEBT.md`](../TECH_DEBT.md); service-specific debt belongs in that service's own `TECH_DEBT.md`.

Everything here sits behind the published-package boundary: these libs are consumed cross-service by
`PackageReference` pinned to `$(ConcertablePlatformVersion)`, so a breaking change can't land atomically —
it needs a publish-first cut-over (see `plans/CLAUDE.md`, "Boundary-blocked refactors"). That constraint is
why several items below are deferred rather than simply fixed.

---

## MED

### Shared test libraries are ProjectReferenced across the service-folder boundary (carve leak)

`Concertable.Testing`, `Concertable.Testing.Integration`, and the shared `Concertable.E2ETests` harness
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
[`plans/SHARED_TEST_LIBS_PACKAGING.md`](../../plans/SHARED_TEST_LIBS_PACKAGING.md). Lean: publish, for
consistency with the Shared-repo model — the cost is that every shared-test-helper edit then takes the
publish-first cycle.

---

## LOW

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
