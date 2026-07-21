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

### `IEntity.DisplayName` is a soft standard (throwing default member), not a hard `static abstract`

`Concertable.Kernel/IEntity.cs` carries `DisplayName` as a `static virtual` **default interface
member** whose default *throws* `NotSupportedException`, so entities that self-name via `OrNotFound()` must
override it; an un-overridden entity fails at runtime rather than the compiler forcing a name. The intended
design was `static abstract` (compiler-enforced, every entity named), but that is a binary-breaking change
that cannot land: the core libs (`DataAccess.Infrastructure`, `Messaging.Domain`) source-reference Kernel
so integration tests load the new Kernel, while service entities compile against the Kernel *package* — a
required static-abstract member's implementation mapping is fixed at compile time against the old interface,
so package-compiled entities throw `TypeLoadException` against the new Kernel (two red CI runs confirmed).
The default member is the additive workaround.

**Resolves when:** the core libs stop source-referencing Kernel (or the repo builds shared source lockstep
so entities compile against the same Kernel the tests load), at which point `DisplayName` can become
`static abstract` and the throwing default is deleted.

### Kernel `ClaimsPrincipal.GetId()` fails open with `string.Empty`

`Concertable.Kernel/Identity/ClaimsPrincipalExtensions.cs` returns `user?.FindFirst("sub")?.Value ?? string.Empty` — a principal with no `sub` claim becomes an empty-string user id instead of a failure. Its sibling `CurrentUserExtensions.GetId(ICurrentUser)` gets this right (throws `UnauthorizedAccessException`). The only consumer, `NotificationHub`, assigns the result to `string?` and null-checks it — a check that can never fire because the method never returns null, so an unauthenticated principal sails through as `""`.

**Resolves when:** the extension fails closed (returns `string?` with no empty-string coercion, or throws like its `ICurrentUser` sibling), and `NotificationHub`'s guard actually rejects principals without a `sub` claim.

---

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
