# ADR 0006 — Soft delete & smart cascade delete

**Status:** Accepted

## Context
Deleting a book with dependent copies (some possibly borrowed), or a member with
borrow history, needs a safe, explicit flow — and nothing should be lost by
accident.

## Decision
- **`Domain.Common.Entity` base class**: GUID identity + a soft-delete lifecycle
  (`IsDeleted` / `DeletedAtUtc`, `MarkDeleted()` / `Restore()`). All four
  aggregates derive from it — one place for identity and deletion semantics.
- **Nothing is physically removed.** An EF global query filter and explicit
  `!IsDeleted` predicates in the in-memory repos hide deleted rows from every
  read, search, duplicate check and aggregate. Freed ISBNs / barcodes /
  membership numbers can be reused.
- **Smart cascade** (`DELETE .../{id}?force=`):
  - a currently-borrowed dependent → **always blocked** (`*_HAS_BORROWED_COPIES`
    / `*_HAS_ACTIVE_BORROW`);
  - dependent data exists and `force=false` → 409 with the exact message and the
    dependent identifiers, asking the caller to confirm (`*_HAS_DEPENDENT_COPIES`
    / `*_HAS_BORROW_HISTORY`);
  - `force=true` → soft-deletes the aggregate and its dependents in one
    transaction.
- Frontend `cascadeDelete()` helper does the two-step confirm automatically.

## Consequences
- Deletes are recoverable and auditable.
- The domain owns its deletion rules (not the controller).
