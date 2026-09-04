# Backend — Developer Guide

.NET 10. See `docs/programmers-guide/` for task guides; this is the quick map.

## Run

```bash
dotnet build LibraryManagementSystem.slnx        # 0 warnings enforced
dotnet test  LibraryManagementSystem.slnx        # 60 tests

# in-memory (no DB):
Database__Provider=InMemory dotnet run --project src/Library.Api

# PostgreSQL:
docker compose up -d db
dotnet run --project src/Library.Api             # migrates + seeds (Development)
```

API on http://localhost:5254 ; OpenAPI/Scalar on `/scalar` (Development).

## Projects

| Project | Contains |
|---|---|
| `Library.Domain` | entities + enums, no dependencies |
| `Library.Application` | feature services, DTOs, `Abstractions/Persistence`, `Common/` (Result, ErrorCodes, validators, `Search/`, `Pagination/`) |
| `Library.Infrastructure` | `Persistence/` (EF context, provider factory, `Repositories/EfCore`, `Repositories/InMemory`, `Migrations`, `Seed`), `Logging/`, `BulkImport/` |
| `Library.Api` | controllers, `Middleware/`, `BackgroundJobs/`, `HealthChecks/`, `Observability/`, `Program.cs` |

## Common tasks

- **New CRUD** → `docs/programmers-guide/02-add-a-crud.md`
- **New search field** → add a `.Field(...)` to the entity's `*SearchMap`
- **New migration** → `MIGRATIONS.md`
- **New background job** → `docs/programmers-guide/07-jobs.md`
- **New config key** → add to the POCO + `appsettings.json` + `11-config.md`

## Rules

- `sealed` classes, primary-constructor DI, `Scoped` services.
- New features return `Result` / `Result<T>` and map with `ToActionResult`.
- Every write goes through `IUnitOfWork.SaveChangesAsync`; multi-entity writes
  use `BeginTransactionAsync`.
- No EF Core types in `Library.Application`.
- Enums: append only, never reorder.
- `TreatWarningsAsErrors` is on — fix warnings, don't suppress.
