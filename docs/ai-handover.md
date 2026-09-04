# AI Handover --- Library Management System

**Last updated:** 2026-09-04
**Written by:** Claude (principal-engineer role), in a sandbox **with a
working .NET 10 SDK, NuGet, Node 26 and Docker** - so unlike the 0.2.0
session, everything below is **built, tested and smoke-verified**.

Read this file first. `docs/ROADMAP.md` and `docs/MASTER_SPECIFICATION.md`
describe the long-term plan; this file is exactly where execution stands.

---

## 0. Current state (verified this session)

| Check | Result |
|---|---|
| `dotnet build LibraryManagementSystem.slnx` | **0 warnings, 0 errors** (TreatWarningsAsErrors on) |
| `dotnet test` | **60 pass** (50 unit + 10 integration), 0 fail |
| `npm run build` / `npm run lint` (frontend) | clean |
| End-to-end (headless browser, API + web) | 0 console errors, 0 failed requests, every page renders and flows work |

Branch: `feat/enterprise-completion` (off `main`). Commits are one-per-milestone
with full messages.

## 1. What was completed this session

### Backend (in-memory persistence, per the user's chosen sequencing)

1. **M0 - green baseline.** Fixed 4 integration tests that broke on the
   0.2.0 enum-as-string change (shared `TestJson` options). Added
   `Directory.Build.props` (`TreatWarningsAsErrors=true`). Gave
   `BookSearchField`/`BookSortField` a real namespace (they had none).
2. **M1 - shared building blocks.** `Result` / `Result<T>`; `ErrorCodes`
   + `SupportedValues` catalog; exception-free `FieldRules` +
   Book/Member/BookCopy validators that run on a candidate struct so an
   API request and an Excel row use the *same* rules; `ValidationException
   -> 422` in the middleware returning the full `ApiErrorResponse.errors[]`
   list; `ResultActionExtensions` (Result -> HTTP status via the standard
   envelope); `GET /api/metadata/enums` + `/search-operators`.
3. **M2 - domain + CRUD + dashboard.**
   - Domain: `MemberStatus.Inactive`; `Member.MembershipExpiresAt` +
     `Deactivate()`/`IsExpired()`/`UpdateProfile()`; `Renew()` extends the
     term; `CanBorrow()` also checks expiry. `BookCopyStatus.Lost/Damaged/
     Maintenance` + `BookCopy.ChangeStatus/ChangeBarcode` with a
     borrowed-copy guard.
   - **Advanced search** (`QueryableSearchBuilder`): hand-built
     `System.Linq.Expressions` over `IQueryable<T>` (parameter-rebinding
     visitor -> EF-translatable later; no dynamic-LINQ, no injection
     surface). Operators eq/neq/contains/notContains/startsWith/endsWith/
     gt/gte/lt/lte/in/notIn/between; AND|OR; multi-sort always after
     filtering; page-size clamp. **Enum fields matched by NAME**
     ("Suspended") with `Enum.GetNames` returned as `supportedValues` on a
     bad value. Per-entity whitelists (`*SearchMap`).
   - `POST /api/{books|members|book-copies|borrowing}/search` +
     querystring `List()` endpoints. Legacy `GET /api/books` preserved.
   - Full Members + BookCopies CRUD (list/search/get/create/update/delete
     + member detail with borrowing summary + copy status transitions),
     duplicate detection returning every error together.
   - `GET /api/dashboard` - one-pass aggregate (no N+1).
4. **M3 - bulk Excel import** (`BulkImportPipeline`, ClosedXML). Books,
   members, book copies. File guard -> workbook read -> header match
   (short-circuit) -> per-row sanitise (whitespace + **formula-injection
   reject**: cells starting `= + @ TAB CR`) -> field validation stamped
   with the Excel row -> in-file duplicate detection -> DB-conflict
   detection -> **preflight gate** -> persist the whole batch or write
   nothing. `POST /api/{...}/import` (multipart) + `GET /api/{...}/import/
   template` (.xlsx with header + examples + Instructions sheet). 9
   acceptance tests cover all ten MASTER_SPECIFICATION §21 scenarios.
5. **M5 - membership maintenance.** `MemberMaintenanceService` (shared by
   the cron job and a new manual endpoint): suspend overdue borrowers +
   mark expired active members Inactive. `POST /api/jobs/member-
   maintenance/run` (403 when the feature flag is off). Cron job now
   delegates to the service. `BorrowingController` dropped its bespoke
   `{message}` try/catch -> central middleware (conflicts now 409 +
   standard envelope).

### Frontend (full design-system rebuild)

Tailwind v4 + TS strict + a small in-house component kit (Badge/StatusPill,
DataTable, AdvancedSearch (GitLab-style filter builder), Pagination, Modal,
BulkImportModal, FormField, …) + a typed API client with `normaliseError()`
mapping `ApiError[]` to per-field and per-row messages + SweetAlert2 +
toasts. Pages rebuilt: **Dashboard** (aggregates + "Run membership
maintenance now"), **Books**, **BookCopies** (inline status transitions),
**Members** (row menu: Suspend/Reactivate/Renew/Mark inactive/Delete),
**MemberDetail**, **Borrowing** (librarian search, not UUIDs). Old per-page
CSS and the stale numeric-enum handling are gone.

## 2. Why these are the right fixes

- **Search as expression trees, not string-parsed dynamic LINQ** - the
  per-entity typed whitelist is provably safe (no arbitrary property or
  method access) and the same builder runs on `AsQueryable()` now and
  translates to SQL under EF Core with zero changes.
- **Import all-or-nothing via a preflight gate, not compensation logic** -
  nothing is inserted until every row passes, so "roll back the whole
  file" is structural, not something that can half-fail. The later EF pass
  only wraps the commit in a real transaction.
- **API contracts frozen now** (error envelope, search request/response,
  paged shape) so the pending EF Core migration is a DI swap, not a
  frontend rewrite.
- **Result on new features, exceptions on old** - avoids a big-bang
  controller rewrite while every new endpoint returns the standard
  `{success:false, errors:[…]}` shape the frontend already handles.

## 3. What is NOT done yet (in priority order)

1. **EF Core + PostgreSQL + provider abstraction + Dapper toggle.** Still
   100% in-memory. Plan: `DatabaseOptions` POCO (`Provider` Postgres(default)
   |SqlServer|MySql|Sqlite|Oracle|InMemory, `Orm` EfCore|Dapper),
   `LibraryDbContext` + configs (enums as string columns, unique indexes,
   `xmin` concurrency, audit cols), provider factory, `IUnitOfWork`/
   `ITransaction`, Ef repositories, `DbCommandInterceptor` -> QueryLogScope,
   DB-down structured build-error diagnostics, Npgsql health check, EF
   seeder reproducing the exact seed literals, Testcontainers integration
   tests. Postgres-only committed migrations; other providers documented.
   `appsettings.{Development,Production}.json` connection strings.
2. **OpenTelemetry + Jaeger** (traces + metrics, OTLP, appsettings-toggled).
3. **Docker**: multi-stage API Dockerfile, frontend Dockerfile + nginx,
   root `docker-compose.yml` (postgres + jaeger + api + web).
4. **CI/CD**: `.github/workflows/ci.yml` (build/test/lint/docker).
5. **Load/stress tests**: `tests/Library.LoadTests` (NBomber).
6. `GET /api/release-notes/current` endpoint (served from a JSON sidecar).
7. Domain fields deferred to keep churn down: `Book.Category`/`Publisher`,
   `Member.Phone`/`Address`. Add with a migration + DTO + template update.
8. Docs: `docs/programmers-guide/*`, root `guide.md` + `MIGRATIONS.md`,
   per-project `DEVELOPERS-GUIDE.md`, ADRs, `docs/database/schema.sql`.

## 4. Exact commands to pick up

```bash
cd ~/Downloads/porosh/LibraryManagementSystem
git checkout feat/enterprise-completion

# backend - confirm green
dotnet build LibraryManagementSystem.slnx      # expect 0/0
dotnet test  LibraryManagementSystem.slnx      # expect 60 pass

# run the API (in-memory) + smoke the new features
dotnet run --project src/Library.Api            # http://localhost:5254  (/scalar for docs)
curl -X POST localhost:5254/api/books/search -H 'content-type: application/json' \
  -d '{"filters":[{"field":"author","operator":"contains","value":"martin"}],"sort":[{"field":"publishedYear","direction":"desc"}]}'
curl -X POST localhost:5254/api/members/search -H 'content-type: application/json' \
  -d '{"filters":[{"field":"status","operator":"in","values":["Suspended","Inactive"]}]}'
curl -X POST localhost:5254/api/jobs/member-maintenance/run
curl -OJ localhost:5254/api/books/import/template
curl -X POST localhost:5254/api/books/import -F file=@book.xlsx

# frontend
cd frontend/library-web && npm install && npm run dev   # http://localhost:5173
# (needs the API running; base URL from .env -> VITE_API_BASE_URL)

# NEXT MILESTONE - EF Core. Start here:
dotnet add src/Library.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Library.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/Library.Api            package Microsoft.EntityFrameworkCore.Design
docker run -d --name lms-pg -e POSTGRES_DB=library -e POSTGRES_USER=library \
  -e POSTGRES_PASSWORD=library -p 5432:5432 postgres:17
# then: LibraryDbContext + configs, DatabaseOptions, provider factory in
# InfrastructureServiceExtensions, swap the InMemory registrations behind
# Provider=InMemory, add IUnitOfWork calls to the services, update the
# unit-test fakes (or use the real InMemory repos - unit tests already
# reference Library.Infrastructure). Keep the frozen API contracts.
dotnet ef migrations add InitialCreate --project src/Library.Infrastructure --startup-project src/Library.Api
```

## 5. Landmines

- **Positional-record DTOs** are consumed positionally in tests - any field
  addition is a compile break across all call sites; do it in one commit.
- **`MemberResponse` gained `MembershipExpiresAt`** as a required positional
  param (before the optional ones); the frontend type already has it.
- Unit-test fakes were extended for the new repo members; `Library.UnitTests`
  now references `Library.Infrastructure`, so new interface members only need
  the real `InMemory*Repository` + the hand fakes updated (or migrate tests
  off the fakes).
- Seed data is deliberately shaped for QA (Dana = Inactive/expired, Erin =
  overdue borrower, one Lost copy); `BooksApiTests` still asserts the exact
  "Clean Code" / `9780132350884` literals - keep them in any EF seeder.
- `BorrowingController` conflict responses moved **400 -> 409**; the frontend
  handles it, but note it for any external consumer.
