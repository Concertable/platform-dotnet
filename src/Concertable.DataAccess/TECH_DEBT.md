# DataAccess technical debt

## Standardize the duplicate-aware insert (distinct from the plain `InsertAsync`)

The shared repository now has a plain `InsertAsync` (add + save, returns the entity, propagates all
failures as exceptions — the create-and-persist / Cosmos `CreateAsync` shape). That name is therefore
taken.

Customer Review and Preference currently duplicate a *different* primitive: immediate, **duplicate-aware**
insert — add, save, return `false` only for the recognized duplicate-key conflict, propagate unrelated
failures. Hoist that primitive to the shared generic DataAccess repository under a distinct name (e.g.
`TryInsertAsync`, matching its `bool` Try-pattern return) so it coexists with `InsertAsync`, and remove
both module-local copies. Do the hoist as a published-package cutover.

**Progress — PR1 (`Chore/TechDebt-dataaccess-tryinsert`) landed the producer half:** `TryInsertAsync`
exists as a `WriteRepositoryExtensions` extension on `IWriteRepository<TEntity>`
(`Concertable.DataAccess.Infrastructure/Extensions/WriteRepositoryExtensions.cs`) — an extension rather
than a new interface member or a third hand-copied implementation on `WriteRepository<TEntity>` /
`Repository<TEntity, TKey>`, since it needs nothing but the two members (`AddAsync` +
`SaveChangesAsync`) those bases already expose publicly; adding it as a class member would have meant
writing the same try/catch twice, compounding the "Repository bases repeat CRUD" item in
[`api/TECH_DEBT.md`](../TECH_DEBT.md) instead of avoiding it. Unit-covered in `RepositoryTests`.
**Remaining — PR2 (delivery-gated on the republish + `platform-sync` pin bump):** migrate
`PreferenceRepository.InsertAsync` and `ConcertReviewRepository.InsertAsync` (Customer) to call the
shared `TryInsertAsync` and delete both module-local copies — `ConcertReviewRepository` doesn't inherit
`Repository<TEntity>`, so it calls the extension directly off its own `context`-backed CRUD, not
through inheritance.

## Standardize the duplicate-aware save (distinct from `TryInsertAsync` above)

`Concertable.B2B.Admin.Infrastructure.Services.AdminService.TrySaveGrantAsync` hand-rolls the same
duplicate-aware shape as the `TryInsertAsync` primitive above — `SaveChangesAsync`, catch
`DbUpdateException` via `IsDuplicateKey()`, `DiscardFailedChanges()`, return `false` — but for a
*save* of already-tracked changes (a race between two calls granting the same admin), not an insert
of a fresh entity. One call site today, so not worth hoisting yet; once a second one shows up,
generalize both into one `TrySaveAsync`/`TryInsertAsync` pair on the shared repository, with the
duplicate-branch behavior as a caller-supplied delegate rather than a fixed `false`. Same shape as
`TryInsertAsync`: `TrySaveAsync` needs nothing but the already-public `SaveChangesAsync`, so it belongs
in `WriteRepositoryExtensions` as an extension too, not a member hand-copied onto
`WriteRepository<TEntity>` / `Repository<TEntity, TKey>` — bolting it onto either base directly would
reintroduce the exact duplication `TryInsertAsync` was hoisted to avoid.

## `RepositoryTests` repeats `InMemoryDatabaseRoot`/`databaseName` arrange lines per test

Every database-touching test in `Concertable.DataAccess.UnitTests/RepositoryTests.cs` opens with the
same two lines — `var root = new InMemoryDatabaseRoot(); var databaseName = Guid.NewGuid().ToString();`
— before calling `CreateContext`/`CreateReadContext`. xUnit gives every `[Fact]` a fresh instance of the
test class, so this pair is safe to hoist into constructor-initialized fields without any cross-test
isolation risk; it would just sit unused (cheaply) in the handful of reflection-only tests
(`Repository_ContextField_UsesCombinedCapabilityOnly` and its two siblings) that touch no database at
all. Not fixed here because the pair repeats across essentially the whole file — a one-line change would
be pure churn; the fix reads better as one pass over every test in the file, not a drive-by edit.

**Resolves when:** `root` and `databaseName` become constructor-initialized fields (or a small
`private (InMemoryDatabaseRoot, string) NewDatabase()` helper if a fresh pair is ever needed mid-test),
and every existing test's arrange section drops its own copy of those two lines.

## `PaginationExtensions.ToPaginationAsync` takes no `CancellationToken`

Every other async repository method reaching I/O threads a `CancellationToken` per the `persistence`
standard; `ToPaginationAsync` (`Concertable.DataAccess.Infrastructure.PaginationExtensions`) does not, so
every paginated repository method built on it inherits the gap — `ModerationController`'s report queue,
both Venue/Artist review repositories, all three Customer review repositories, and
`VenuePrivilegedRepository.GetPendingApprovalAsync` (added for the admin-console venue-approval queue,
`plans/launch/ADMIN_CONSOLE_PLAN.md` Phase 4) are today's known instances.

**Resolves when:** `ToPaginationAsync` gains a `CancellationToken ct = default` parameter, threaded
through to its underlying query execution, and every caller listed above (plus any added meanwhile) is
updated to pass its own `ct` through. A shared-package change spanning every consuming service, so it
ships as its own published-package cutover, not a drive-by on any one caller's PR.

## `IReadRepository.GetByIdAsync` should not be `virtual`

`GetByIdAsync` is `virtual` on the read base, and repos override it to eager-load a relation
(`ConcertReadRepository` → `Include(Genres)`, `CommissionBindingRepository` →
`Include(CommissionConfiguration)`). That makes `GetByIdAsync(id)` return different shapes per repo, so a
caller can't trust what it gets — a surprising-behaviour smell. Cross-aggregate/projection reads already
live under explicit names (`GetDtoAsync`, `GetDetailsAsync`, `GetDetailsByIdAsync`); GetById should mean
"the bare aggregate," predictably.

Seal `GetByIdAsync` (remove `virtual`) and migrate the overrides: EF `.AutoInclude()` on the model for a
genuinely owned relation (the aggregate always loads its parts, no override needed), or an explicitly
named method for anything cross-aggregate. Touches the read bases plus the two overriding consumers
across services, so it's its own change, not part of the base-unify PR.
