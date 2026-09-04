# Library Management System — Start-to-Finish Guide

A step-by-step walkthrough of running, using, testing and observing the system.

---

## 1. What's in the box

| Part | Path | Stack |
|---|---|---|
| Backend API | `src/Library.{Domain,Application,Infrastructure,Api}` | .NET 10, EF Core, in-memory/PostgreSQL |
| Frontend | `frontend/library-web` | React 19, Vite, Tailwind, TanStack Query |
| Tests | `tests/Library.{UnitTests,IntegrationTests}` | xUnit |
| Docs | `docs/` + `MIGRATIONS.md` + this file | Markdown |

---

## 2. Run everything with Docker (recommended)

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| Web UI | http://localhost:8080 |
| API + OpenAPI (Scalar) | http://localhost:5254 / http://localhost:5254/scalar |
| Jaeger (traces) | http://localhost:16686 |
| PostgreSQL | localhost:5432 (`library` / `library`) |

The API migrates and seeds the database on first start. Stop with
`docker compose down` (add `-v` to wipe the database volume).

---

## 3. Run locally (no Docker)

### Backend

```bash
# Option A — with PostgreSQL:
docker compose up -d db
dotnet run --project src/Library.Api            # http://localhost:5254

# Option B — zero dependencies (in-memory):
Database__Provider=InMemory dotnet run --project src/Library.Api
```

### Frontend

```bash
cd frontend/library-web
npm install
npm run dev                                     # http://localhost:5173
```

`frontend/library-web/.env` points the SPA at `http://localhost:5254/api`.

---

## 4. Using the app

1. **Dashboard** — live totals (books, copies by status, members by status,
   overdue borrows) plus **Run membership maintenance** to suspend overdue
   borrowers and mark expired memberships inactive immediately.
2. **Books / Book Copies / Members** — each page has:
   - a **quick search** box and a **Filters** panel (GitLab-style: add
     `field / operator / value` rows, AND or OR, multi-field sort);
   - **status badges** (colour-coded);
   - **Bulk import** — download the `.xlsx` template, fill it, upload. If any
     row is invalid or duplicated the whole file is rejected and every error is
     shown with its exact row, field and accepted values.
3. **Members** — the row menu has Suspend / Reactivate / Renew / Mark inactive /
   Delete; click a name for the member detail (borrowing summary + history).
4. **Borrowing** — search a member and an available copy by name/barcode (no
   UUIDs), pick a due date, confirm. Return from the active-borrows table.

---

## 5. Tests

```bash
dotnet test LibraryManagementSystem.slnx        # unit + integration (60)

cd frontend/library-web
npm run lint && npm run build                   # frontend gate
```

See `tests/DEVELOPERS-GUIDE.md`.

---

## 6. Observability

- **Traces** — set `FeatureFlags:EnableOpenTelemetry = true` (compose does this)
  and open Jaeger at http://localhost:16686; pick service `Library.Api`. Each
  request is a trace with child DB spans.
- **Structured file logs** — `logs/` under the API content root:
  - `runtime-error-logs/` — unhandled 500s + background-job faults
  - `build-error-logs/` — startup failures (incl. the diagnosed DB-down cause)
  - `query-logs/` — every SQL command (text, params by name/type, duration, provider)
  - `exception-logs/` — expected business exceptions mapped to 4xx
  Download a day's file via `GET /api/logs/available` then `/api/logs/download`.
- **Health** — `GET /health` reports the persistence provider and, if it can't
  connect, why.

---

## 7. Migrations & databases

See `MIGRATIONS.md`. Switch provider with `Database:Provider`
(`Postgres` primary, also `SqlServer`, `Sqlite`, `InMemory`).

---

## 8. Where to go next

- `docs/ROADMAP.md` — phase status and what's left
- `docs/ai-handover.md` — current execution state + exact next commands
- `docs/programmers-guide/` — how to add a CRUD, a cron job, a migration, etc.
