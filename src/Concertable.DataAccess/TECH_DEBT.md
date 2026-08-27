# DataAccess technical debt

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

## `PaginationExtensions.ToPaginationAsync` takes no `CancellationToken`

Every other async repository method reaching I/O threads a `CancellationToken` per the `persistence`
standard; `ToPaginationAsync` (`Concertable.DataAccess.Infrastructure.PaginationExtensions`) does not, so
every paginated repository method built on it inherits the gap — `ModerationController`'s report queue,
both Venue/Artist review repositories, all three Customer review repositories, and
`VerificationRepository.GetPendingAsync` (the tenant-verification admin queue) are today's known instances.

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
