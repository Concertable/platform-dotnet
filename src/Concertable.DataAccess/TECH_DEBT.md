# DataAccess technical debt

## Standardize the duplicate-aware save (distinct from `TryInsertAsync` above)

`Concertable.B2B.Admin.Infrastructure.Services.AdminService.TrySaveGrantAsync` still hand-rolls the
duplicate-key save path. The shared `TrySaveChangesAsync` handles EF concurrency only; it must not swallow
every `DbUpdateException` or accept a caller-supplied exception-policy delegate merely to absorb this
provider-specific case.

**Resolves when:** duplicate-key save handling has a clean shared primitive and B2B deletes its private
method.

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
