# DataAccess technical debt

## Standardize immediate duplicate-aware inserts

Customer Review and Preference Phase 8 introduces duplicate immediate, duplicate-aware `InsertAsync`
primitives: add the entity, save immediately, return `false` only for the recognized duplicate-key
conflict, and propagate unrelated failures. After that feature lands, move the primitive to the shared
generic DataAccess repository through a published-package cutover and remove both module-local copies.
