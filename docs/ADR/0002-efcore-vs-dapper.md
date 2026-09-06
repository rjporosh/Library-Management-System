# ADR 0002 — EF Core vs Dapper

**Status:** Accepted

## Context
The spec asks for "EF Core for rich persistence, Dapper for optimized read /
reporting workloads where justified", switchable via `Database:Orm`.

## Decision
- **Writes and advanced search always use EF Core.** Change tracking, the
  transaction abstraction and the safe expression-tree translation are EF's
  strengths; a second write path or SQL generator would double the surface.
- **`Database:Orm=Dapper` routes the dashboard aggregate** through
  `DapperDashboardReadStore` — one round-trip of SQL `COUNT`/`CASE` aggregates
  instead of materialising every copy, member and borrow row. It is the one
  genuine "reporting" query in the system.
- Dapper reads are supported on PostgreSQL and SQLite; other relational
  providers fall back to the EF read store even under `Orm=Dapper`.
- `IDashboardReadStore` has three implementations (EF, Dapper, in-memory) and a
  parity integration test proves EF and Dapper produce identical numbers.

## Consequences
- The Dapper slot is real and extensible (add a read store per reporting query)
  without a parallel query engine.
- The toggle is safe: it never affects correctness, only how the aggregate is
  computed.
