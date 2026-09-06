# AI Handover --- Library Management System

**Last updated:** 2026-09-06 (fourth checkpoint - OpenAPI titles/descriptions
now rendered, Postman collection with runnable examples)
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
| `dotnet test` | **78 pass** (57 unit + 21 integration), 0 fail |
| `npm test` (frontend, Vitest) | **7 pass** |
| `npm run build` / `npm run lint` (frontend) | clean |
| End-to-end (headless browser, API + web) | 0 console errors, 0 failed requests, every page renders and flows work |
| `docker compose up --build` | all 4 services up; `/health` "Postgres database is reachable"; web proxies API; Jaeger receives `Library.Api` traces |
| EF Core against a real PostgreSQL 17 | migration applies, seed runs, advanced search translates to SQL (enum-by-name), bulk-import rollback, query log written |

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

## 3. Completed since the first checkpoint

- **EF Core + PostgreSQL primary + provider abstraction.** `DatabaseOptions`,
  `LibraryDbContext` (enums as string columns, unique indexes, FKs, composite
  indexes, shadow audit cols), `DatabaseProviderConfigurator` (Postgres/
  SqlServer/Sqlite; MySql/Oracle/Access/Mongo throw), `IUnitOfWork`/
  `ITransaction` (+ `NoOpUnitOfWork` for in-memory), `Ef*Repository`,
  `QueryLoggingInterceptor`, startup DB-down diagnostics, real
  `PersistenceHealthCheck`, `DatabaseSeeder`, `InitialCreate` migration,
  design-time factory. Integration tests pinned to InMemory via
  `LibraryApiFactory`. `appsettings.{Development,Production}.json`.
- **OpenTelemetry + Jaeger** - `OpenTelemetryExtensions`, appsettings-toggled.
- **Docker** - API + web Dockerfiles, `nginx.conf`, `docker-compose.yml`
  (db + jaeger + api + web), `.dockerignore`.
- **CI** - `.github/workflows/ci.yml` (backend build/test + ef drift check,
  frontend lint/build, docker image builds + compose smoke test).
- **Load tests** - `tests/Library.LoadTests` (NBomber, 3 scenarios).
- **Docs** - `guide.md`, `MIGRATIONS.md`, `docs/programmers-guide/` (12 files),
  per-project `DEVELOPERS-GUIDE.md`, `docs/database/schema.sql`.

## 3c. Completed in the third pass

- **Soft delete + smart cascade delete** - `Domain.Common.Entity` base;
  `IsDeleted` filter everywhere; `DELETE ...?force=` blocked/confirm/force flow
  for books, members, copies; frontend `cascadeDelete()` two-step helper.
- **`Book.Category`/`Publisher`, `Member.Phone`/`Address`** - full stack
  (domain, DTOs, validators, Excel templates, seed, frontend forms + search).
- **Dapper read-path** - `IDashboardReadStore` (EF / Dapper / in-memory),
  `IDbConnectionFactory`, parity integration test.
- **Rate limiting** (per-client fixed-window, forwarded-header aware, 429
  problem+json), **RFC 7807** problem+json envelope, **AddProblemDetails**.
- **Localization** - `Resources/SharedResources[.bn].resx`, RequestLocalization,
  `GET /api/metadata/messages`, frontend message map + EN/বাংলা switch.
- **Frontend Vitest** suite (wired into CI); +5 backend integration tests.
- **Docs** - 7 ADRs, C4 (Mermaid), ER diagram, `seed-data.sql`,
  localization guide, updated config reference.
- InitialCreate migration regenerated; `SeedData` extracted (one dataset for
  both seeders).

## 3d. What is NOT done yet (all optional / follow-ups)

1. **MySql / Oracle** EF drivers when EF Core 10-compatible packages ship
   (Pomelo 9 needs EF Core 9; no EF10 Oracle provider). Provider slots + ADR-0001
   are ready - one `case` in `DatabaseProviderConfigurator` + a package.
2. Dapper coverage beyond the dashboard (a `Dapper*ReadStore` per additional
   reporting query, same pattern).
3. Success response envelope (`{success,message,data,traceId}`) - deferred by
   ADR-0005; opt-in filter or major version.
4. More Bangla coverage in the resx (only the error catalogue + system messages
   are translated; UI chrome strings are still English in the SPA).
5. Broader frontend component tests (only lib + one component covered).

## 4. Exact commands to pick up

```bash
cd ~/Downloads/porosh/LibraryManagementSystem
git checkout feat/enterprise-completion

# backend - confirm green
dotnet build LibraryManagementSystem.slnx      # expect 0/0
dotnet test  LibraryManagementSystem.slnx      # expect 78 pass
( cd frontend/library-web && npm ci && npm test && npm run build )   # 7 tests, clean

# run the API - Development uses Postgres (docker compose up -d db first);
# for a no-database demo:  Database__Provider=InMemory dotnet run --project src/Library.Api
dotnet run --project src/Library.Api            # http://localhost:5254  (/scalar for docs)
curl -X POST localhost:5254/api/books/search -H 'content-type: application/json' \
  -d '{"filters":[{"field":"author","operator":"contains","value":"martin"}],"sort":[{"field":"publishedYear","direction":"desc"}]}'
curl -X POST localhost:5254/api/members/search -H 'content-type: application/json' \
  -d '{"filters":[{"field":"status","operator":"in","values":["Suspended","Inactive"]}]}'
curl -X POST localhost:5254/api/jobs/member-maintenance/run
curl -OJ localhost:5254/api/books/import/template
curl -X POST localhost:5254/api/books/import -F file=@book.xlsx

# full stack
docker compose up --build     # web :8080, api :5254, Jaeger :16686, db :5432

# frontend only
cd frontend/library-web && npm install && npm run dev   # http://localhost:5173

# The enterprise scope is complete. Remaining items (§3d) are optional
# follow-ups - MySQL/Oracle drivers when packages ship, more Dapper read
# stores, the deferred success envelope, wider Bangla / frontend-test coverage.
# Suggested next: merge feat/enterprise-completion to main after review.
```

## 4b. Fourth checkpoint (this session)

- **OpenAPI/Scalar now shows every endpoint's title + description.** Root cause:
  `Directory.Build.props` left `GenerateDocumentationFile` off, so the .NET 10
  OpenAPI XML-comment source generator had no doc file to read - all 43
  summaries were blank despite the `///` comments existing. Fix: enabled
  `<GenerateDocumentationFile>` + `<NoWarn>CS1591;CS1573;CS1572;CS1570;CS1734</NoWarn>`
  in `src/Library.Api/Library.Api.csproj` (0-warning gate intact). Also fixed a
  duplicated stale doc block on `BooksController.Delete` (repeated
  `<response code>` made the generator's `SingleOrDefault` throw and 500 the
  whole `/openapi/v1.json`) and a malformed `</   param>` on `GetAll`.
  Verify: `curl -s localhost:5254/openapi/v1.json | jq '[.paths[][] .summary] | map(select(.==null))'` -> `[]`.
- **`postman/`** - v2.1 collection, all 43 endpoints in 11 folders with example
  bodies + a runnable "Smoke Flow" folder (verified `newman run` 10/10
  assertions). Regenerate with `python3 postman/build_collection.py`.
- `BooksController` route normalised `api/[controller]` -> `api/books`
  (case-insensitive routing, no consumer impact).
- Verified: build 0/0, unit 57/57, integration 21/21, newman smoke 9/9.

## 4c. Startup DB fix (this session)

Symptom: `dotnet run` crashed with an unhandled
`Npgsql.PostgresException 42P07: relation "books" already exists` when the
target database already had the tables but an empty (or missing)
`__EFMigrationsHistory` - e.g. a DB built from `docs/database/schema.sql`, an
old `EnsureCreated()`, or a restored dump.

- **`src/Library.Infrastructure/Persistence/DatabaseBootstrapper.cs`** (new) -
  replaces the bare `db.Database.MigrateAsync()`. Ensures the history table
  exists, then: if there are pending migrations, no applied migrations and the
  DB already has tables, it **baselines** every migration (writes the history
  rows) instead of re-running `CREATE TABLE`. Then migrates normally.
- **`Program.cs`** - migrate step now calls `DatabaseBootstrapper.MigrateAsync`;
  the catch writes the one-line diagnostic then `Environment.Exit(1)` (no more
  raw stack dump). New diagnostic branch for `42P07` / "already exists".
- Tests: `tests/Library.IntegrationTests/Persistence/DatabaseBootstrapperTests.cs`
  (empty DB migrates; schema-without-history adopts). 23 integration tests now.
- Verified on real Postgres 16: fresh DB, schema-with-empty-history, and
  schema-with-no-history-table all boot healthy and seed; DB-down exits 1 with
  the diagnostic.
- `MIGRATIONS.md` + `guide.md` §7 now carry the add-migration / update-db /
  regenerate-schema / drop commands, all runnable from the repo root.

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
