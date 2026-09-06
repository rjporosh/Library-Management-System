# Library Management System

Enterprise library management platform — .NET 10 (Clean Architecture, EF Core,
PostgreSQL) + React 19 / Vite SPA, fully containerised with observability.

## Run the whole thing (one command)

```bash
docker compose up --build
```

Brings up four containers — PostgreSQL, Jaeger, the API, and the web UI. The API
migrates and seeds the database on first start.

| Service | URL |
|---|---|
| Web UI | http://localhost:8080 |
| API (OpenAPI / Scalar) | http://localhost:5254 · http://localhost:5254/scalar |
| Jaeger (traces) | http://localhost:16686 |
| PostgreSQL | `localhost:5432` — `library` / `library` |

Stop with `docker compose down` (add `-v` to wipe the database volume).

> It is a multi-container app, not a single image — the database, tracing
> backend, API and SPA each run in their own container, wired together by
> `docker-compose.yml`. `docker compose up` is the single command that runs
> them all.

## Run without Docker

```bash
# API — zero dependencies (in-memory store)
Database__Provider=InMemory dotnet run --project src/Library.Api

# API — with PostgreSQL
docker compose up -d db
dotnet run --project src/Library.Api            # http://localhost:5254

# Frontend
cd frontend/library-web && npm install && npm run dev   # http://localhost:5173
```

## Documentation

| Doc | Contents |
|---|---|
| [`guide.md`](guide.md) | start-to-finish: run, use, test, observe |
| [`MIGRATIONS.md`](MIGRATIONS.md) | every `dotnet ef` command, from the repo root |
| [`docs/programmers-guide/`](docs/programmers-guide/) | architecture, add a CRUD, search, jobs, errors, localization, config, troubleshooting |
| [`docs/database/`](docs/database/) | `schema.sql`, `seed-data.sql`, ER diagram |
| [`docs/ADR/`](docs/ADR/) | architecture decision records |
| [`postman/`](postman/) | importable API collection + runnable smoke flow |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) · [`docs/RELEASE-NOTES.md`](docs/RELEASE-NOTES.md) · [`docs/ai-handover.md`](docs/ai-handover.md) | status |

## Test

```bash
dotnet test LibraryManagementSystem.slnx                 # 57 unit + 23 integration
cd frontend/library-web && npm test                      # Vitest
dotnet run --project tests/Library.LoadTests -c Release  # NBomber load test
```

## Build gate

`Directory.Build.props` sets `TreatWarningsAsErrors` — the solution builds with
**0 warnings, 0 errors**. CI (`.github/workflows/ci.yml`) runs backend
build+test, frontend lint+test+build, image builds and a compose smoke test.
