# DataAccess technical debt

## Integration test seam blocks any binary-breaking change to the published base types

Integration tests run consumers **compiled against the feed** `Concertable.DataAccess.*` package while
the host loads the **source-built** DataAccess.dll (`Seed.Infrastructure` ProjectReferences the source
project; its higher MinVer wins assembly resolution). So a change to a published base type that consumer
IL depends on — a moved field's declaring type, a renamed/removed type — makes old-compiled IL meet the
new assembly and dangle (`FieldAccessException`/`TypeLoadException`), failing the PR's own integration
suites. It can therefore never reach the platform-sync that would recompile consumers. This is why the
`Repository : ReadRepository` reparent had to be reverted and why `IBaseRepository`→`IWriteRepository`
(below) can't ride a normal PR.

The durable fix: make the integration harness build consumers against the **same** DataAccess the host
loads (compiled == runtime) — a local pack + pin override, or a source ProjectReference gated to the test
build only. It must not violate the carve rule forbidding a production ProjectReference to source
DataAccess across the `api/Concertable.B2B/` boundary. Until this lands, every base-type change must be
binary-additive, or shipped as a deprecate→migrate→remove publish-first sequence.

## Rename `IBaseRepository`/`BaseRepository` → `IWriteRepository`/`WriteRepository`

`IBaseRepository` is a misleading name for what is actually the **write-only facet**
(Add/AddRange/Insert/Update/Remove/SaveChanges, no reads, no key — Cosmos calls it `IWriteOnlyRepository`).
The facet is load-bearing (keyless `SequenceRepository`, plus `OpportunitySyncer`/`CollectionSyncer`), so
it stays — but the name should be honest. Deferred because renaming a published type is binary-breaking
(feed-compiled consumers reference it by name), so it hits the test-seam wall above. Land it either after
the seam is fixed (then it's one PR) or as a deprecate→migrate→remove publish-first sequence. Low value on
its own (cosmetic); best folded into the seam fix.

## Standardize the duplicate-aware insert (distinct from the plain `InsertAsync`)

The shared repository now has a plain `InsertAsync` (add + save, returns the entity, propagates all
failures as exceptions — the create-and-persist / Cosmos `CreateAsync` shape). That name is therefore
taken.

Customer Review and Preference Phase 8 introduces a *different* primitive: immediate, **duplicate-aware**
insert — add, save, return `false` only for the recognized duplicate-key conflict, propagate unrelated
failures. When that feature lands, hoist that primitive to the shared generic DataAccess repository under
a distinct name (e.g. `TryInsertAsync`, matching its `bool` Try-pattern return) so it coexists with
`InsertAsync`, and remove both module-local copies. Do the hoist as a published-package cutover.

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
