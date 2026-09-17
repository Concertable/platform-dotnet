# Code review — Refactor/PostgresPlatformReplacement

> **This file is a work order, not a discussion.** If you're handed this file, fix the open `[ ]`
> findings directly and report what changed. Tick each `[x]` as you land it. Pause only for a genuinely
> irreversible or ambiguous finding: record its durable disposition, take the safe path, and keep going.

**Review status:** `complete`
**Reviewed up to commit:** `db92326` `(2026-09-17)`
**Judgment:** `approved`

## Review pass — 2026-09-17 — full

**Candidate base:** `13e68b4577ee1360f7c99e33a0c452df8dad4457`
**Candidate head:** `ebe78e9a37b29fa1f842085b88d32bcefeeeb280`
**Candidate branch:** `Refactor/PostgresPlatformReplacement`
**Candidate scope:** `all`
**Candidate path-set:** `sha256:3bcc220109a56aa1b5f343b779d68bd3d5937eb9953514325ef0c48d1beba827` `(80 paths)`
**Candidate bundle:** removed after completion
**Candidate bundle identity:** `sha256:5f5268cfae1ec5eb4637d511aff379a3729900e91120c027e86dd615bae4782e`
**Work-order path:** `reviews/Refactor-PostgresPlatformReplacement.md`
**Work-order mode:** `new`
**Pass judgment:** `changes-requested`

Lenses: native-general, correctness (claim/lease/savepoint semantics), repository conventions,
changed-behaviour test impact. All findings below were resolved in the remediation commit.

### Findings

- [x] **R1 — HIGH — correctness** — `src/Concertable.Messaging/Concertable.Messaging.Infrastructure/Outbox/OutboxDispatcher.cs:75`
  Making `Status`/`NextRetryAtUtc` concurrency tokens turned one lost lease into a whole-batch loss: the
  drain mutates every claimed row and saves once, so the first conflicted row rolled back the outcome of
  every other row in the batch. Two replicas slower than the lease would then redeliver the same batch
  forever without ever incrementing `Attempts`, so dead-lettering could never fire. Fixed:
  `SaveOutcomesAsync` detaches the entries a `DbUpdateConcurrencyException` names, logs
  `OutboxCompletionLost`, and re-saves the rest, so a worker loses only its own reclaimed rows.
  `DrainOnce_OneRowOfTheBatchReclaimedMidDrain_StillCommitsTheRest` pins it.

- [x] **R2 — HIGH — correctness** — `src/Concertable.DataAccess/Concertable.DataAccess.Infrastructure/Extensions/DbSetExtensions.cs:24`
  The savepoint was created but never released after a rollback — PostgreSQL keeps a rolled-back savepoint
  alive, so a seed loop stacked one subtransaction per contested call and crossed the 64-subxid cliff. Any
  failure that was *not* a duplicate key also escaped with the savepoint open, leaving the caller's
  transaction aborted where SQL Server had left it usable. Fixed: every exit rewinds and releases, and a
  non-duplicate `DbException` rethrows after the rewind.

- [x] **R3 — MEDIUM — test-impact** — `src/Concertable.Shared/tests/Concertable.Testing.Integration.IntegrationTests/ResetScopeFixture.cs:43`
  `ResetAsync_WhatPostgisInstalled_IsLeftStanding` could not fail: the fixture never named `public` as an
  owned schema, so Respawn would not have touched `spatial_ref_sys` even with the ignore entry deleted.
  Fixed: the fixture owns `public` and keeps a sentinel table there, so the ignore entry is load-bearing
  and a new case proves the reset still empties what the service owns in that schema.

- [x] **R4 — MEDIUM — test-impact** — `.../Concertable.Messaging.IntegrationTests/OutboxReaderTests.cs:43`
  The batch-bound test asserted a count and never an identity, so deleting `ORDER BY "OccurredAtUtc", "Id"`
  from the claim — and starving the oldest messages forever — stayed green. Fixed: the rows are seeded
  newest-first and the test asserts the two oldest are the ones claimed.

- [x] **R5 — MEDIUM — test-impact** — `.../OutboxReaderTests.cs:109`
  The contention test could not distinguish `SKIP LOCKED` from plain serialisation: neither reader held a
  transaction, so one claim taking everything satisfied both assertions. Fixed: the first claim is held
  open in an explicit transaction and the second must return a disjoint, equally sized slice within a
  bounded deadline rather than block on it.

- [x] **R6 — MEDIUM — test-impact** — `.../OutboxReaderTests.cs:125`
  Reclaim was only exercised a full second past the lease, so flipping `<=` to `<` passed. Fixed: paired
  cases at exactly the deadline and one second before it.

- [x] **R7 — MEDIUM — test-impact** — `.../OutboxReaderTests.cs`
  Only `Dispatched` was proved un-claimable at the store; a `DeadLettered` row was never tested, so
  renumbering `OutboxStatus` could redispatch dead letters forever with every test green. Fixed.

- [x] **R8 — MEDIUM — test-impact** — `src/Concertable.DataAccess/Concertable.DataAccess.Infrastructure/DbContextBase.cs:19`
  Nothing asserted that the new `IOptions<OutboxOptions>` parameter changes the schema the base actually
  maps — only that the option could be set. Fixed, and writing it surfaced that EF keys its model cache on
  the context type, so the schema is fixed by whichever options instance built the model first. Recorded in
  `src/Concertable.Messaging/TECH_DEBT.md` along with the migration and inbox still pinned to `messaging`.

- [x] **R9 — MEDIUM — test-impact** — `.../Concertable.Messaging.IntegrationTests/MessageStoreMigrationTests.cs`
  The regenerated migrations were never applied twice, which the plan names as a gate. Fixed: a rerun
  asserts nothing further is pending, one history row per store, and the outbox landing in its schema.

- [x] **R10 — MEDIUM — conventions** — comments
  Three added comments failed the zero-comment bar: `PostgresFixture`'s stacked `//` design narration on a
  const, the same text duplicated into `AddPostgresContainer`'s `<summary>` while documenting a different
  member, and an explanatory `<summary>` on the new `AddOutbox` overload in DI composition code. All
  deleted — the reasoning is in the commit message. The savepoint comment earns its place (a concrete
  engine footgun with its SQLSTATE) and was compressed to one line.

- [x] **R11 — MEDIUM — conventions** — `.../Concertable.DataAccess.UnitTests/WriteRepositoryExtensionsTests.cs:49`
  `ConflictingDbContext` captured `inner` through a primary constructor, which the style standard forbids
  for captured state and the project rules forbid outright on a context. Fixed here and on
  `ConsumerDbContext`, which the same diff had left disagreeing with its new sibling `ReferenceDbContext`.

- [x] **R12 — MEDIUM — conventions** — extension containers
  `DbSetExtensions` and `DbUpdateExceptionExtensions` had their bodies rewritten but stayed legacy
  `this`-parameter statics beside a brand-new `extension()` sibling in the same folder. Both migrated. The
  larger `DistributedApplicationBuilderExtensions` mixing predates this cutover and is recorded in
  `src/Concertable.AppHost.Shared/TECH_DEBT.md` rather than swept mid-migration.

- [x] **R13 — LOW — conventions** — the PostGIS image coordinate stood in three hand-maintained copies.
  The unit test's literals now read the AppHost constants through `InternalsVisibleTo`; the fixture's copy
  cannot reference them without pulling `Aspire.Hosting` into every service test project, so it is recorded
  as debt.

- [x] **R14 — LOW — conventions** — `__EFMigrationsHistory_Outbox` was a bare literal in
  `OutboxDispatcherTests` beside the fixture constant that derives it, and `"References"` was a literal
  beside a constant-supplied schema. Both now come from their owning constant.

- [x] **R15 — LOW — conventions** — `OutboxServiceCollectionExtensionsTests` initialised its collaborator in
  a field initializer instead of the test constructor.

- [x] **R16 — LOW — conventions** — `MessageStoreFixture.CreateOutboxContext` took an optional
  `OutboxOptions` that all 22 call sites ignored.

- [x] **R17 — LOW — conventions** — `AddDatabase_KeepsTheNameTheCompositionAsksFor` had a two-segment name
  and sat under `#region AddPostgresContainer` though its method under test is `AddDatabase`.

- [x] **R18 — LOW — conventions** — `MessageStoreModelTests` qualified field access with `this.` in method
  bodies, where the rest of the repository qualifies only in constructors.

- [x] **R19 — LOW — test-impact** — the per-checkout volume test would have passed on a random suffix, which
  is the exact failure its twelve-line doc comment says must not happen. Fixed: two builders in one process
  must produce the same volume name.

- [x] **R20 — LOW — conventions** — `Concertable.DataAccess.IntegrationTests` gained a container fixture and
  two test classes but had no `AGENTS.md`, unlike every other test project.

- [x] **R21 — LOW — conventions** — `OutboxDispatcherTests` hand-rolled nine registrations `AddOutbox`
  already owns and reached `DrainOnceAsync` by reflection. It now drives the real registration, and
  `DrainOnceAsync` is `internal` behind the `InternalsVisibleTo` the same change added.

- [x] **R22 — LOW — test-impact** — `RelationalTestDatabaseExtensions`'s check-constraint helpers lost their
  provider switch for hardcoded PostgreSQL SQL that no test had ever executed; a syntax error would have
  surfaced only in a downstream service cutover. Four cases now exercise both helpers against a real
  container.

### Dispositions — deliberately unchanged

- `runDispatcher: false` reads as a named argument that exists only to skip `configure`. The remedy the
  style standard names is a focused overload, which would widen a published contract for a test's
  convenience; the toggle is genuine and naming it is the readable form. No change.
- `Concertable.Seed.Shared` keeps `Microsoft.EntityFrameworkCore.SqlServer` and its SQL-Server-only
  identity-insert rewriter. Deleting `SeedingIdentityInterceptor`/`UseSeedingSupport` is a public-API break
  on a package outside this slice's enumerated ownership and outside the four-package contract; the
  rewriter is inert on Npgsql (`RequiresIdentityInsert` returns false). Carried to the plan ledger as the
  one surviving SQL Server reference beyond the allowlisted duplicate classifier.
