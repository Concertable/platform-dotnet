# DataAccess technical debt

## Standardize the duplicate-aware save (distinct from `TryInsertAsync` above)

`Concertable.B2B.Admin.Infrastructure.Services.AdminService.TrySaveGrantAsync` and
`Concertable.B2B.Application.Infrastructure.Services.ApplicationWorkflow` (the apply-duplicate and
accept-duplicate paths) all hand-roll the same `catch (DbUpdateException ex) when (ex.IsDuplicateKey())`
save path. The shared `TrySaveChangesAsync` handles EF concurrency only; it must not swallow every
`DbUpdateException` or accept a caller-supplied exception-policy delegate merely to absorb this
provider-specific case — that would turn one closed, well-known failure category into an open "any error,
caller decides" API that lets a call site quietly swallow something it shouldn't.

The shape to add instead mirrors the sibling `WriteRepositoryExtensions.TryInsertAsync` in this same file:
a `DbContext` extension (e.g. `TrySaveChangesOrDuplicateAsync`) that catches only the duplicate-key case,
calls `DiscardFailedChanges()`, and returns `Result<Unit, DuplicateKeyError>` — not a delegate, not a bool.
Every other `DbUpdateException` still propagates. Callers map the typed `DuplicateKeyError` to their own
operation-specific error.

**Resolves when:** duplicate-key save handling has this shared `Result`-typed primitive and B2B deletes
every hand-rolled instance in favor of it.

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
