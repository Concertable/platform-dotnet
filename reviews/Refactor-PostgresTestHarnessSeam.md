# Code review — Refactor/PostgresTestHarnessSeam

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `3653aa8` `(2026-09-15)`
**Judgment:** `approved`

## Review pass — 2026-09-15 — full

**Candidate base:** `3136a4ee10be52dbea6a9badc56e3140b025ff20`
**Candidate head:** `457d45135773837b059007464dfaf5e0fb5dd8b1`
**Candidate branch:** `Refactor/PostgresTestHarnessSeam`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:fe16318e4cf99dc1b14e032e763e91ff7cdb4e3f60e804fa4d56e2c9b91fa044` `(12 paths)`
**Candidate bundle:** removed after completion
**Candidate bundle identity:** `sha256:fe16318e4cf99dc1b14e032e763e91ff7cdb4e3f60e804fa4d56e2c9b91fa044`
**Work-order path:** `reviews/Refactor-PostgresTestHarnessSeam.md`
**Work-order mode:** `new`
**Pass judgment:** `changes-requested`

Lenses: native-general, correctness (reset semantics), repository conventions, changed-behaviour test
impact. All eight findings were resolved in `3653aa8`.

### Findings

- [x] **R1 — MEDIUM — test-impact** — `src/Concertable.Shared/tests/Concertable.Testing.Integration/SqlFixture.cs:12`
  `SqlFixture` had no test of its own, though the parts that matter need no container: a declared provider
  reaching `ActiveProvider`, the variable overriding it, a non-name being refused, resolution happening
  once, and the no-argument reset refusing on Postgres. Fixed: `SqlFixtureTests` pins all five, subclassing
  the fixture and never starting a container, so no banned unit-tier symbol is touched.

- [x] **R2 — LOW — naming** — `.../DatabaseProviderSelector.cs:10`
  The type exposed `Resolve` and `Parse` and never selected anything; `NAMING.md` names a single-operation
  type for its method's agent noun (`Resolver.Resolve`). Fixed: renamed to `DatabaseProviderResolver`.
  `OwnedSchemaSelector` keeps its name — `Select` is what it does.

- [x] **R3 — LOW — correctness** — `.../SqlFixture.cs:77`
  `ReadSchemaCatalogAsync` dereferenced a null `dbConnection` when a caller reached the database before
  `InitializeAsync`, giving a bare `NullReferenceException` where every other new guard gives a diagnostic.
  Fixed: a `Connection` property names the skipped step; used by both reset overloads and `ResetAsync`.

- [x] **R4 — LOW — test-impact** — `.../DatabaseProviderSelectorTests.cs:36`
  `Resolve` was never exercised with a negative number, though `Parse` was. Fixed: `[InlineData("-1")]`.

- [x] **R5 — LOW — test-impact** — `.../DatabaseProviderSelectorTests.cs:51`
  Nothing pinned that the declared default is validated *before* an invalid override is parsed, so the two
  error paths could have swapped unnoticed. Fixed: `Resolve(Undeclared, "Oracle")` must fault on `99`, not
  on `Oracle`.

- [x] **R6 — LOW — comments** — `.../SqlFixture.cs:116-117`
  The PostGIS-image comment was two stacked `//` lines; `COMMENTS.md` allows one `//` or a single `/* */`
  block, never stacked. Content keeps its place (an external packaging fact). Fixed: collapsed to one line.

- [x] **R7 — LOW — conventions** — `Concertable.Platform.slnx:53`, `src/Concertable.Shared/Directory.Packages.props:18`, `.../Concertable.Testing.Integration.csproj:17`
  Three manifests are maintained in alphabetical order and each new entry broke it. Fixed: moved to slot.

- [x] **R8 — LOW — unit-testing** — `.../OwnedSchemaSelectorTests.cs:59`
  `Select_TheSchemaAService_CanOwnNamedLikeADefault_Keeps` carried four underscore segments against
  `UNIT.md`'s `Method_Scenario_ExpectedBehaviour`. Fixed: renamed to three.

### Reported and rejected

- **No-arg overload omits `messaging.__EFMigrationsHistory_Inbox`/`_Outbox`** — rejected on a false premise.
  Plan §6 gives those separate history names only to the *Postgres* message stores ("Preserve the existing
  SQL history layout; only the new Postgres stores receive separate named histories"), and the no-argument
  overload refuses Postgres, so they can never be in its scope. A live SQL Server container confirmed all
  three histories (`dbo`, `search`, `messaging`) survive that overload's reset.

- **`TablesToIgnore` protects identities that do not exist, so real histories get deleted** — the premise is
  half right and the conclusion wrong. Nothing in-tree creates those two names yet; they are a forward
  reference to plan §6, which §4 explicitly instructed ("including existing `__EFMigrationsHistory` and new
  names below"), and a Respawn ignore for an absent table is inert. The predicted loss cannot occur because
  every owned schema's own `__EFMigrationsHistory` is excluded by the per-schema loop; a live PostGIS
  container confirmed the `public`, `search` and `messaging` histories all survive.

- **XML-doc summaries should open with a noun phrase, not a verb** — `COMMENTS.md` phrases this as a
  preference, and in-repo precedent is verb-first (`ConcurrencyConflictInterceptor`: "Turns a race into…").

- **Braces required on an `if` whose single statement wraps** — `STYLE.md` cites
  `csharp_prefer_braces = when_multiline`, but this repo's `.editorconfig` does not set it and the
  prevailing local style is braceless (`AzureServiceBusReceiver.cs:158`, `ServiceCollectionExtensions.cs:33`).

## Review pass — 2026-09-15 — incremental

**Candidate base:** `457d45135773837b059007464dfaf5e0fb5dd8b1`
**Candidate head:** `3653aa8`
**Candidate branch:** `Refactor/PostgresTestHarnessSeam`
**Candidate scope:** `all`
**Candidate path-set:** remediation delta for R1–R8 (9 paths)
**Work-order path:** `reviews/Refactor-PostgresTestHarnessSeam.md`
**Work-order mode:** `append`
**Pass judgment:** `approved`

### Findings

No findings. The delta is confined to the eight resolutions above. Re-verified at this head: solution build
clean (0 warnings, 0 errors), 67 unit tests pass, and all three container smokes pass — PostGIS
(two reset cycles, histories and `spatial_ref_sys` preserved, identities reseeded), SQL Server (legacy
no-argument overload unchanged plus the scoped overload), and the environment-override rejections
(every non-name refused with no container created).
