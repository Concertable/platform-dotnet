# DataAccess technical debt

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
