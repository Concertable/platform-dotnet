# Code review — Refactor/PostgresProviderNeutralPersistence

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `d28c806` `(2026-09-16)`
**Judgment:** `approved`

## Review pass — 2026-09-16 — full

**Candidate base:** `fdea4621afd3581f1f948d6362b465a5b165a753`
**Candidate head:** `1e0a78f970940c409dca6ed11b1eac73c2f36e4f`
**Candidate branch:** `Refactor/PostgresProviderNeutralPersistence`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:04abb084aafdaa2306938c7635eb9ccd9476852b7afa9a0831f62a8f25908425` `(24 paths)`
**Candidate bundle:** removed after completion
**Work-order path:** `reviews/Refactor-PostgresProviderNeutralPersistence.md`
**Work-order mode:** `new`
**Pass judgment:** `changes-requested`

Lenses: native-general, provider-migration correctness, repository conventions, changed-behaviour test
impact. Six findings, all resolved in `cba8516`. No security-qualifying path in the frozen set, so no
security marker is stamped.

### Findings

- [x] **R1 — MEDIUM — naming** — `src/Concertable.Messaging/Concertable.Messaging.Infrastructure/MessageStoreMappingExtensions.cs:6`
  The new class was `EntityTypeBuilderExtensions`, which is already the name of a public static class in
  `Concertable.Kernel` that also extends `EntityTypeBuilder<T>`. Both ship in packages every service
  consumes together, so a `DbContext` importing both namespaces and naming either class statically would
  not compile. Extension invocation syntax resolves per method, which is why the build stayed green and
  no lens caught it. Fixed: renamed to `MessageStoreMappingExtensions`, which names what it maps.

- [x] **R2 — MEDIUM — test-impact** — `src/Concertable.Messaging/Tests/Concertable.Messaging.UnitTests/MessageStoreMappingExtensionsTests.cs`
  Nothing asserted the mapping the branch exists to correct. Fixed: over a provider-less model, each
  bounded column declares its length and no column type, each unbounded one declares neither, and the
  dispatch index is on `Status`, `OccurredAtUtc`. A reintroduced `HasColumnType` fails these.

- [x] **R3 — MEDIUM — test-impact** — `src/Concertable.DataAccess/Tests/Concertable.DataAccess.IntegrationTests/MessageStoreModelTests.cs`
  Nothing proved `DbContextBase` still routes through those extensions rather than growing its own copy
  again — the exact drift that put `nvarchar` in two places. Fixed: against a relational model, a derived
  context must keep the lengths, keep the index, and name no column type that is not the provider's own.
  The same class pins that the messaging tables and their index stay out of a consumer's generated schema,
  which is what stops every service that writes to the shared stores from creating them a second time.

- [x] **R4 — LOW — test-impact** — `src/Concertable.DataAccess/Tests/Concertable.DataAccess.UnitTests/WriteRepositoryExtensionsTests.cs`
  `TryInsertAsync`'s duplicate branch had no test because `SqlException` cannot be constructed.
  `PostgresException` can. Fixed: the branch returning `false` and detaching is covered, alongside a
  foreign-key violation that must still throw.

- [x] **R5 — LOW — test-impact** — `src/Concertable.Shared/tests/Concertable.AppHost.Shared.UnitTests/PostgresContainerResourceTests.cs:44`
  `WithPostGis` replaces the image on a resource `AddPostgresContainer` has already given a per-checkout
  volume, and the doc comment tells callers to chain them, but nothing held the volume across the swap.
  Fixed.

- [x] **R6 — LOW — comments** — `src/Concertable.AppHost.Shared/DistributedApplicationBuilderExtensions.cs:45`
  The `AddPostgresContainer` summary opened by narrating its relationship to `AddSqlServerContainer`,
  which is the design narration the comment rule excludes. Fixed: the PostGIS sentence, which carries a
  reason a caller needs, is what remains.

### Recorded, not fixed here

- **Outbox schema divergence** — `src/Concertable.DataAccess/Concertable.DataAccess.Infrastructure/DbContextBase.cs:32`
  `DbContextBase` hardcodes `MessagingSchema.Name` while `OutboxMessageEntityConfiguration` resolves the
  schema from `IOptions<OutboxOptions>.SchemaName`. Real and reachable, dormant only because the option
  defaults to that same constant. The line is unchanged by this candidate, and unifying it changes a
  published abstract base-class constructor, so it is a package cutover rather than a drive-by. Recorded
  in `src/Concertable.DataAccess/TECH_DEBT.md`.

- **`OutboxReader` non-SQL branch has no atomic claim and no lease reclaim** —
  `src/Concertable.Messaging/Concertable.Messaging.Infrastructure/Outbox/OutboxReader.cs:27`
  Untouched by this candidate and already specified as Phase 4B work in plan section 6, which names the
  `FOR UPDATE SKIP LOCKED` claim, the `Dispatching`-plus-expired-lease eligibility clause and the
  concurrency predicates. Left with its owner.

### Rejected

- **Extension idiom on `DbUpdateExceptionExtensions`** — the `this`-parameter signature line is unchanged
  by this candidate, and that form is the repository majority (37 files to 15). A pre-existing choice on
  an unchanged line against a convention the repository does not uniformly hold.

- **Arrange/Act/Assert comments on the four appended entity tests** — the surrounding tests in those two
  files carry them, but `// Arrange` narrates the *what* and carries no reason a reader needs at that
  line, which the comment rule excludes outright. The new tests stay uncommented, as do the other new
  test files and the existing `PropertyBuilderExtensionsTests`.

### Verification at the reviewed head

- `dotnet build Concertable.Platform.slnx` — 0 warnings, 0 errors.
- `dotnet test Concertable.Platform.slnx --configuration Release` — 640 passed, 0 failed, 13 projects.
- `initial-migrations.ps1 -Check` — no pending model changes for either context.
- `dotnet pack Concertable.Platform.Packages.slnx` + `eng/verify-build-law.py` — 3 of 3 rules arrive.
