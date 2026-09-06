# ADR 0003 — Advanced search: hand-built expression trees

**Status:** Accepted

## Context
GitLab-style search needs arbitrary field/operator/value filters combined with
AND/OR and multi-sort, over four entities, translatable to SQL.

## Decision
- `QueryableSearchBuilder` composes `System.Linq.Expressions` from **typed
  accessor lambdas** held in a per-entity `SearchFieldMap`. A `ParameterRebinder`
  visitor inlines the accessors so the predicate is EF-translatable.
- **No `System.Linq.Dynamic.Core`, no string-to-expression parsing.** The value
  of dynamic LINQ is arbitrary user expressions — exactly what must not be
  allowed. The whitelist gives provable safety: no property or method outside
  the map can be reached.
- The operator set is bounded (13). Enum fields are matched by name. Unknown
  field / disallowed operator / unparseable value returns a precise `ApiError`
  with the accepted values.
- Filtering is always applied before sorting and paging.

## Consequences
- The same builder runs on `AsQueryable()` (in-memory, unit-tested) and under EF
  Core with zero changes.
- Adding a searchable field is one `.Field(...)` line.
