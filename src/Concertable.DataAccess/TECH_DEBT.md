# DataAccess technical debt

## Standardize the duplicate-aware save (distinct from `TryInsertAsync` above)

`Concertable.B2B.Admin.Infrastructure.Services.AdminService.TrySaveGrantAsync` still hand-rolls the
duplicate-key save path. The shared `TrySaveChangesAsync` resets its unit of work after every
`DbUpdateException`; Admin does not yet use that unit-of-work boundary.

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

## `DbContextBase` hardcodes the messaging schema the outbox configuration makes configurable

`DbContextBase.OnModelCreating` maps the outbox table at `MessagingSchema.Name`, while the authoritative
`OutboxMessageEntityConfiguration` resolves it from `IOptions<OutboxOptions>.SchemaName`. Both agree today
only because the option defaults to the same constant. A service that set `SchemaName` would write outbox
rows through its own `DbContextBase`-derived context into `messaging` while `OutboxReader` polls the
configured schema — messages that are never dispatched, with nothing raised. Only the column mappings were
unified; the schema was not.

**Resolves when:** both mapping paths take the schema from one source. `DbContextBase` is a published
abstract base every service derives from, so threading the option through it changes a published
constructor signature — it ships as its own package cutover, not a drive-by.
