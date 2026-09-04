# RELEASE NOTES --- Library Management System

This file is the human-readable release history for the Library
Management System.

The API endpoint:

``` http
GET /api/release-notes/current
```

shall expose the current release in a machine-readable format for SQA
and release verification.

------------------------------------------------------------------------

# Release 0.3.0 --- Advanced Search, Bulk Import & MVP Frontend

**Release date:** 2026-09-04\
**Status:** Built, tested (60 tests), smoke-verified end to end\
**Release type:** Feature (MVP completion)

## Purpose

Deliver the two headline outstanding features (GitLab-style advanced
search, all-or-nothing Excel bulk import) and a production-grade
frontend, so the system is fully demoable on the in-memory provider
before the persistence layer is introduced.

## New features

- **Advanced search** on Books, Members, Book Copies and Borrow Records
  via `POST /api/{resource}/search`: multiple `{field, operator, value}`
  filters combined AND or OR, 13 operators (eq/neq/contains/notContains/
  startsWith/endsWith/gt/gte/lt/lte/in/notIn/between), multi-field sort
  (always applied after filtering), pagination. Enum/status fields are
  matched by name ("Suspended"); an unknown field/operator/value returns
  a precise error with the accepted values. Built on a safe hand-written
  expression-tree builder (no dynamic-LINQ, EF-translatable).
- **Bulk Excel import** for Books, Members and Book Copies. `POST
  /api/{resource}/import` (multipart .xlsx); `GET /api/{resource}/import/
  template` downloads a formatted template. All-or-nothing: file/header
  validation, per-row field validation, formula-injection rejection,
  in-file and database duplicate detection, then a preflight gate - if
  any row fails, nothing is written and every error is returned together
  with its exact Excel row, field, code, message and accepted values.
- **Membership maintenance**: expired active members are now moved to
  `Inactive` (new status) alongside overdue-borrower suspension.
  `POST /api/jobs/member-maintenance/run` triggers the pass manually.
- **Full CRUD + list/search parity** for Members and Book Copies,
  member detail with borrowing summary/history, and book-copy condition
  transitions (Lost/Damaged/Maintenance).
- `GET /api/dashboard` - single-call aggregate snapshot (no N+1).
- `GET /api/metadata/enums` / `/search-operators` for the UI.
- **Result pattern**, stable `ErrorCodes` catalog, and shared field
  validators used by both the CRUD endpoints and the import pipeline.
  Validation failures return HTTP 422 with the full error list.
- **New frontend** (React 19 + Tailwind v4): typed API client with
  field- and row-level error mapping, SweetAlert2, a component kit
  (status badges, sortable data table, GitLab-style advanced-search
  builder, bulk-import modal with an error table), and rebuilt pages -
  Dashboard (with the manual maintenance button), Books, Book Copies,
  Members (with lifecycle actions), Member detail, Borrowing (librarian
  search rather than UUIDs).

## Changed behaviour

- `BorrowingController` conflicts now return **HTTP 409** with the
  standard `ApiErrorResponse` envelope (previously 400 + `{message}`).
- `MemberResponse` gained `membershipExpiresAt`.
- Enums `MemberStatus` (+`Inactive`) and `BookCopyStatus`
  (+`Lost`/`Damaged`/`Maintenance`) - append-only.

## Fixed

- Four integration tests that failed after the 0.2.0 enum-as-string
  change (the test client now deserializes enums by name).
- The 0.2.0 backend is now actually built and tested (it never was).

## Added after the first 0.3.0 cut (same release line)

- **EF Core + PostgreSQL (primary) + provider abstraction.** `Database:Provider`
  selects `InMemory` (default) / `Postgres` / `SqlServer` / `Sqlite` by config
  only; MySql/Oracle/Access/Mongo are acknowledged slots that fail clearly.
  `LibraryDbContext` (enums as string columns, unique indexes, FKs, audit
  columns), `IUnitOfWork`/`ITransaction`, `InitialCreate` migration,
  design-time factory, EF seeder. `MIGRATIONS.md` at the repo root.
- **DB-down diagnostics** - a startup connection failure is classified (server
  unreachable / database missing / auth failed / schema stale) and written to
  `logs/build-error-logs/` with the provider, host, database and a fix hint.
- **Query logging** - `DbCommandInterceptor` writes every SQL command (text,
  parameter names+types only, duration, rows, provider) to `logs/query-logs/`.
- **OpenTelemetry** - traces + metrics over OTLP to Jaeger, toggled by
  `FeatureFlags:EnableOpenTelemetry`.
- **Docker** - multi-stage API + web Dockerfiles, `docker-compose.yml`
  (postgres + jaeger + api + web); `docker compose up --build`.
- **CI** - `.github/workflows/ci.yml` (build/test + migration drift check,
  frontend lint/build, image builds + compose smoke test).
- **Load tests** - `tests/Library.LoadTests` (NBomber, 3 scenarios).
- **Docs** - `guide.md`, `docs/programmers-guide/` (12 guides), per-project
  `DEVELOPERS-GUIDE.md`, `docs/database/schema.sql`.
- **`GET /api/release-notes/current`** - machine-readable current release
  (served from `src/Library.Api/release-notes.json`) for SQA / release checks.

## Known limitations

- `Database:Orm=Dapper` currently falls back to EF Core (Dapper read stores pending).
- No EF Core 10 driver for MySQL/Oracle yet (provider slots + docs are ready).
- `Book.Category`/`Publisher` and `Member.Phone`/`Address` deferred.
- No frontend unit tests; rate limiting / RFC 7807 / localization not started.

## QA checklist

- [x] `dotnet build` 0 warnings / 0 errors
- [x] `dotnet test` 60/60
- [x] advanced search: operators, enum-by-name, unknown-field error
- [x] bulk import: all 10 §21 acceptance scenarios
- [x] manual maintenance job, member lifecycle transitions
- [x] frontend build + lint clean; end-to-end browser check (0 console errors)

------------------------------------------------------------------------

# Release 0.2.0 --- Observability, Cron & Member Lifecycle

**Release date:** 2026-09-03\
**Status:** In progress (backend written, NOT YET BUILT/TESTED -\
see AI Handover note at the end of this file)\
**Release type:** Feature + hardening

## Purpose

Close the "ops readiness" gap identified in the 0.1.0 hardening
checklist: centralized exception handling, structured file logging,
a scheduled job for member suspension, a health-check endpoint, and
a way to download log files for manual inspection - all toggleable
per-feature from `appsettings.json`.

## Included

### Business rule fixes
- **One active borrow per member.** `BorrowingService.IssueAsync` now
  rejects a second issue while a member already has a book out
  (`InvalidOperationException`, mapped to HTTP 409).
- **Member status is now a real lifecycle**, not just a flag:
  `Suspend()` / `Reactivate()` / `Renew()` on the `Member` entity,
  each stamping `SuspendedAt` / `LastRenewedAt`. New endpoints:
  `POST /api/members/{id}/suspend`, `/reactivate`, `/renew`.
- **Enum values now serialize as names, not numbers**
  (`JsonStringEnumConverter` registered globally) - this was silently
  broken before; the frontend `Member.status: string` type was
  actually receiving an integer.

### Centralized exception handling
- `CorrelationIdMiddleware` - tags every request/response/log entry
  with an `X-Correlation-Id`.
- `GlobalExceptionHandlingMiddleware` - anticipated exceptions
  (`KeyNotFoundException` -> 404, `ArgumentException` -> 400,
  `InvalidOperationException` -> 409) get the standard
  `ApiErrorResponse` envelope and log to `exception-logs`; anything
  unexpected returns the generic support message below and logs
  full diagnostic detail to `runtime-error-logs`. **Existing
  controllers that already had their own try/catch are untouched**
  (no regression risk) - this middleware is the safety net for
  everything else and the pattern new endpoints should rely on.
- User-facing message for unexpected errors: *"Something went wrong.
  Please contact service provider MD. IKRAMUL ISLAM SIDDIQUE POROSH,
  phone: +8801672896992 for details."*

### Structured file logging (4 categories, JSON-lines)
- `logs/runtime-error-logs/runtime-error-logs-dd-MM-yyyy.txt`
- `logs/build-error-logs/build-error-logs-dd-MM-yyyy.txt`
- `logs/query-logs/query-logs-dd-MM-yyyy.txt`
- `logs/exception-logs/exception-logs-dd-MM-yyyy.logs`

Each entry carries file name/location, method name, cron-job name
(when applicable), line number, root cause, a suggested fix,
generated query text, execution start/end time and elapsed
milliseconds - so slow queries and faults can be diagnosed from the
files alone. Startup failures (DB down, bad config) are caught
around `Program.cs`'s `builder.Build()`/`app.Run()` and logged to
`build-error-logs` via a DI-independent fallback writer.

### Scheduled job
- `MemberSuspensionCronJob` runs once daily at local midnight,
  suspends members with an overdue active borrow, and logs its own
  query + any faults. Fully toggleable and self-disabling via
  `FeatureFlags.EnableMemberSuspensionCronJob`.

### Operability endpoints
- `GET /health` - structured JSON health report (extensible
  `PersistenceHealthCheck`, placeholder until a real DB lands).
- `GET /api/logs/available?category=` - lists downloadable log
  files.
- `GET /api/logs/download?category=&date=` - downloads the exact
  dated log file (path-traversal guarded).

### Configuration
New `FeatureFlags` section in `appsettings.json` toggles every
feature above independently: `EnableRuntimeErrorLogging`,
`EnableBuildErrorLogging`, `EnableQueryLogging`,
`EnableExceptionLogging`, `EnableMemberSuspensionCronJob`,
`EnableHealthCheckEndpoint`, `EnableLogDownloadEndpoint`, and
`LogsRootPath`.

### Tests
- Updated `FakeMemberRepository` / `FakeBorrowRecordRepository` test
  doubles for the new repository members.
- New unit tests: `SuspendAsync`/`ReactivateAsync`/`RenewAsync`
  behavior, and `IssueAsync` rejecting a second concurrent borrow.

## Explicitly NOT included in this release
- Bulk Excel import/export.
- Advanced GitLab-style multi-field/operator search (equals/not
  equals/contains/excludes) with independent sort-before/after-filter
  ordering.
- Full Members/Book-Copies CRUD+search parity with Books.
- OpenTelemetry/Jaeger tracing.
- Dapper/EF Core + multi-RDBMS provider abstraction.
- Frontend SweetAlert wiring for the generic error message (backend
  message exists; frontend interceptor not yet written).
- **Build/test verification** - see AI Handover note below.

------------------------------------------------------------------------

## AI Handover Note (2026-09-03)

This release was written in a sandboxed session with **no .NET SDK
and no NuGet access**, so none of the C# above has been compiled or
tested by the agent that wrote it. Everything was written to
compile-correct standards and manually re-read line-by-line for
syntax errors, but it must be verified before being trusted. See
`docs/ai-handover.md` for the exact commands and the full list of
what to check first.


**Release date:** 2026-08-31\
**Status:** MVP hardening / baseline\
**Release type:** Development baseline

## Purpose

Establish the first functional Library Management System baseline
covering the core OOP/OOAD domain and web application.

## Included

### Backend/domain

-   Book domain
-   Book copy domain
-   Member domain
-   Borrow record domain
-   Book APIs
-   Book-copy APIs
-   Member APIs
-   Borrowing APIs
-   In-memory persistence for MVP development
-   Unit tests
-   Integration tests

### Frontend

-   Dashboard
-   Books
-   Book details
-   Book copies
-   Members
-   Borrowing
-   React + TypeScript + Vite frontend
-   API integration
-   Pagination/search/sort UI foundation

## Current hardening items

The following are explicitly tracked for completion before the MVP is
declared production-ready:

-   complete member borrowing history/details
-   complete book-copy borrower/due-date information
-   complete issue workflow UX
-   complete return workflow UX
-   align frontend search fields with backend
-   implement GitLab-style multi-select search
-   implement SweetAlert2 behavior
-   complete dashboard live member/recent-borrowing information
-   add category and publisher
-   add member phone and address
-   standardize API validation/error contract
-   add Excel bulk import
-   add transaction rollback behavior
-   add exact row/field error reporting
-   add release-notes endpoint
-   add enterprise documentation

------------------------------------------------------------------------

# Next Release --- MVP Completion

**Version:** 0.2.0\
**Release date:** TBD\
**Status:** Planned

## New features

-   GitLab-style multi-select book search
-   Title/Author/ISBN search
-   Multi-field OR search
-   Improved member search
-   Complete member details
-   Borrowing history
-   Complete book-copy availability information
-   Improved issue workflow
-   Improved return workflow
-   SweetAlert2 success/error/confirmation flows
-   standardized validation responses
-   supported-values metadata

## Fixes

-   Frontend/backend search-field mismatch
-   unsupported sort/search options
-   incomplete dashboard values
-   UUID-centric librarian workflows
-   missing member/book relationship data

## QA checklist

### Build

-   [ ] `npm run build` succeeds
-   [ ] backend builds successfully
-   [ ] unit tests pass
-   [ ] integration tests pass

### Search

-   [ ] title search
-   [ ] author search
-   [ ] ISBN search
-   [ ] title + author
-   [ ] title + ISBN
-   [ ] all three
-   [ ] no selected field defaults to title
-   [ ] case-insensitive
-   [ ] whitespace trimmed
-   [ ] pagination works with search
-   [ ] sorting works with search

### Borrow

-   [ ] active member can borrow
-   [ ] inactive/suspended member cannot borrow
-   [ ] available copy can be issued
-   [ ] borrowed copy cannot be issued again
-   [ ] successful issue shows SweetAlert

### Return

-   [ ] active borrowing can be returned
-   [ ] copy becomes available
-   [ ] returned borrow cannot be returned twice
-   [ ] successful return shows SweetAlert

### Error UX

-   [ ] exact field error shown
-   [ ] exact error code available
-   [ ] supported values/rules shown
-   [ ] no false success alert

------------------------------------------------------------------------

# Future Release --- Enterprise Architecture

**Version:** 1.0.0\
**Release date:** TBD\
**Status:** Planned

## New features

-   relational database persistence
-   EF Core
-   Dapper read paths where justified
-   Vertical Slice Architecture
-   CQRS
-   MediatR
-   domain invariants
-   Result pattern
-   centralized exception handling
-   structured logging
-   correlation IDs
-   OpenTelemetry observability
-   health checks
-   resilience policies
-   database migrations
-   SQL schema
-   SQL seed data
-   C4 diagrams
-   ADRs

## Bulk import

-   formatted Excel template
-   file validation
-   header validation
-   sanitization
-   normalization
-   complete pre-validation
-   duplicate detection
-   transaction-backed all-or-nothing import
-   exact row number
-   exact field
-   exact error message
-   exact error code
-   exact supported values/constraints

## QA release verification

-   [ ] migration verified
-   [ ] seed data verified
-   [ ] search regression verified
-   [ ] borrow/return regression verified
-   [ ] transaction rollback verified
-   [ ] observability verified
-   [ ] release-notes endpoint verified
-   [ ] API documentation verified

------------------------------------------------------------------------

# Release Notes Contract

Every future release entry must contain:

1.  Version
2.  Release date
3.  Release status
4.  New features
5.  Fixes
6.  Changed behavior
7.  Known issues
8.  QA checklist
9.  Regression areas
10. Database/migration notes
11. API contract changes
12. Breaking changes, if any

------------------------------------------------------------------------

# Release Notes API Contract

Endpoint:

``` http
GET /api/release-notes/current
```

Example:

``` json
{
  "version": "0.2.0",
  "releaseDate": "2026-09-XX",
  "newFeatures": [
    "GitLab-style multi-select search",
    "Complete member borrowing history",
    "SweetAlert2 workflow feedback"
  ],
  "fixed": [
    "Frontend/backend search contract mismatch",
    "Incomplete issue and return workflows"
  ],
  "qaChecklist": [
    "Verify multi-field OR search",
    "Verify issue/return state transitions",
    "Verify SweetAlert confirmation and success",
    "Verify exact validation errors"
  ]
}
```

The production endpoint must return the actual deployed
version/date/data rather than this placeholder example.

------------------------------------------------------------------------

# Release Policy

A release must not be marked complete simply because code was merged.

A release is complete only after:

``` text
Implementation
   ↓
Build
   ↓
Automated tests
   ↓
Integration verification
   ↓
Manual QA checklist
   ↓
Documentation
   ↓
Release notes
   ↓
Release endpoint
   ↓
Release
```

No partial bulk-import commit is acceptable.

No known frontend/backend contract mismatch is acceptable.

No success notification may be shown before the corresponding operation
actually succeeds.

------------------------------------------------------------------------

# Versioning

Use Semantic Versioning:

``` text
MAJOR.MINOR.PATCH
```

-   **MAJOR:** breaking API/product contract
-   **MINOR:** backward-compatible feature
-   **PATCH:** backward-compatible bug/security/fix release

Every release must record its exact release date.
