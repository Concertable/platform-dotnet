# DataAccess technical debt

## Standardize immediate duplicate-aware inserts

Customer Review and Preference currently duplicate an immediate, duplicate-aware `InsertAsync`
primitive: add the entity, save immediately, return `false` only for the recognized duplicate-key
conflict, and propagate unrelated failures. Move that primitive to the shared generic DataAccess
repository through a future published-package cutover, then remove the module-local implementations.
