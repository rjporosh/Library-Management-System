# Programmer's Guide

Short, task-focused guides for working on the Library Management System.

| Guide | |
|---|---|
| [01 — Architecture & layering](01-architecture.md) | how the projects fit together |
| [02 — Add a CRUD feature](02-add-a-crud.md) | entity → repo → service → controller → tests |
| [03 — Advanced search](03-advanced-search.md) | the `QueryableSearchBuilder` and per-entity field maps |
| [04 — Bulk import](04-bulk-import.md) | the all-or-nothing pipeline and `IImportDefinition<T>` |
| [05 — Persistence & providers](05-persistence.md) | EF Core, `DatabaseOptions`, switching providers |
| [06 — Migrations](../../MIGRATIONS.md) | exact `dotnet ef` commands (repo root) |
| [07 — Background jobs & cron](07-jobs.md) | `BackgroundService`, the maintenance job, manual triggers |
| [08 — Observability](08-observability.md) | OpenTelemetry, the file log streams, `/health` |
| [09 — Error handling & the Result pattern](09-errors.md) | `Result<T>`, `ApiError`, the middleware, status mapping |
| [10 — Testing](../../tests/DEVELOPERS-GUIDE.md) | running and writing tests |
| [11 — Configuration reference](11-config.md) | every `appsettings` key |
| [12 — Troubleshooting](12-troubleshooting.md) | common failures and fixes |
