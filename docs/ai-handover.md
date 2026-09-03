# AI Handover --- Library Management System

**Last updated:** 2026-09-03 (session ending; token budget exhausted)
**Written by:** Claude (principal-engineer role), in a sandbox with
**no .NET SDK and no NuGet access** - see "Critical caveat" below.

Read this file first, before `docs/ROADMAP.md` or
`docs/MASTER_SPECIFICATION.md`. Those two describe the long-term
plan; this file describes exactly where execution actually stands
right now and what to do next.

---

## 0. Critical caveat - READ THIS FIRST

Everything in this session was written **without ever running
`dotnet build` or `dotnet test`**. The sandbox this agent ran in has
no .NET SDK installed and its network is allow-listed to
npm/pypi/github only (not nuget.org), so the backend could not be
compiled, run, or tested at all. Every file was re-read manually for
syntax correctness (brace/paren balance, using statements, method
signatures matching call sites), but **that is not a substitute for
an actual build**. Treat all backend claims below as "should compile,
unverified" until you run the commands in Section 4.

## 1. What was requested (full scope, for context)

The user asked for a very large enterprise hardening pass in one go:
bulk Excel import w/ transactional rollback, GitLab-style advanced
multi-field/operator search with independent sort/filter ordering,
full CRUD+search parity for Members and Book Copies, a "one book per
member" borrowing rule, a midnight cron job to suspend overdue
members with renew/reactivate actions, status shown by name not
number (incl. searchable by name), centralized exception handling
with 4 categories of dated log files (exact schema below), a
configurable feature-flag system, a health-check endpoint, a
log-download-by-filename endpoint, OpenTelemetry/Jaeger
observability, EF Core + Dapper with a multi-RDBMS abstraction
(MSSQL/Postgres/MySQL/Oracle/Access), 100%-passing unit/integration/
load/stress/performance tests, a frontend SweetAlert fallback with a
specific support message, and a `/release-notes` endpoint - all
without breaking OOP/OOAD, without introducing CQRS/vertical-slice
architecture, and without regressions.

This is genuinely multiple weeks of work. It was scoped into phases
(see `docs/ROADMAP.md`, written in an earlier session) and tackled
incrementally, one confirmed priority at a time, per explicit
agreement with the user.

## 2. What is DONE in this session (Release 0.2.0 vertical)

The user explicitly chose this order: **logging middleware + cron +
health check first**, backend code delivered here for the user to
build/test locally (user's explicit choice - see Section 4).

### 2.1 Business-rule fixes (found while in the code, fixed because they were cheap + directly relevant)
- `BorrowingService.IssueAsync` (`src/Library.Application/Features/Borrowing/BorrowingService.cs`) now throws `InvalidOperationException` if the member already has an active borrow (`IBorrowRecordRepository.HasActiveBorrowAsync`). **Root cause:** the check simply didn't exist before. **Why this fix:** it's the one-line, low-risk version of exactly what was asked ("member can take only 1 book at a time"); no repository/schema change needed for the in-memory store.
- `Member` entity (`src/Library.Domain/Entities/Member.cs`) gained `SuspendedAt`, `LastRenewedAt`, `Reactivate()`, `Renew()` (existing `Suspend()` now stamps `SuspendedAt`).
- All enums now serialize as **names**, not numbers (`JsonStringEnumConverter` added in `Program.cs`). **Root cause:** System.Text.Json's default enum serialization is numeric; the frontend's `Member.status: string` type was silently receiving an integer. **Why this fix:** it's the correct fix at the correct layer (serialization, not per-DTO mapping) and directly required by the user ("ui showing the status with name not number").

### 2.2 Centralized exception handling
- `src/Library.Api/Middleware/CorrelationIdMiddleware.cs` - generates/propagates `X-Correlation-Id`.
- `src/Library.Api/Middleware/GlobalExceptionHandlingMiddleware.cs` - maps `KeyNotFoundException`->404, `ArgumentException`->400, `InvalidOperationException`->409 to the standard envelope (`src/Library.Application/Common/Errors/ApiErrorResponse.cs`) and logs to `exception-logs`; anything else -> generic support message + full detail to `runtime-error-logs`.
- **Why not touch existing controllers' try/catch (Books/BorrowingController/BookCopies)?** They already catch and shape their own error responses and existing integration tests assert on their status codes. Rewriting them risked a regression for zero behavioral gain this round. This middleware is strictly additive - the safety net for anything NOT already handled, and the pattern new code (Members suspend/reactivate/renew) already relies on. **Left for next agent:** migrate those controllers to rely solely on the central middleware once you've confirmed test coverage would survive it (fast-follow, not urgent).

### 2.3 Structured file logging
- `src/Library.Application/Common/Logging/` - `LogCategory`, `AppLogEntry` (every field the user asked for), `IAppLogWriter`, `QueryLogScope` (timing helper).
- `src/Library.Infrastructure/Logging/FileAppLogWriter.cs` - JSON-lines writer, one file per category per day:
  - `logs/runtime-error-logs/runtime-error-logs-dd-MM-yyyy.txt`
  - `logs/build-error-logs/build-error-logs-dd-MM-yyyy.txt`
  - `logs/query-logs/query-logs-dd-MM-yyyy.txt`
  - `logs/exception-logs/exception-logs-dd-MM-yyyy.logs`
  Never throws (falls back to stderr) so a logging failure can't crash a request.
- Startup/bootstrap failures (DB down, bad config, DI failure) are caught around `builder.Build()` / `app.Run()` in `Program.cs` and logged via a **DI-independent** fallback (`WriteBuildErrorFallback`) since the DI container - and therefore `IAppLogWriter` - may not exist yet at that point. **Note:** "build-error" here means *startup/bootstrap* failure, not a compiler error - a running application cannot observe its own compile errors; that's a build-time (CI) concern, not a runtime logging concern. Flag this distinction to the user if they push back.
- Query logging is wired into `MemberSuspensionCronJob` only (a safe, new code path) to prove the pattern end-to-end without touching existing, already-tested repository methods (e.g. `BookRepository`'s search). **Left for next agent:** wire `QueryLogScope` into the real query paths once Dapper/EF Core (Phase 11) replace the in-memory LINQ scans - that's when "find slow queries" actually becomes meaningful.

### 2.4 Scheduled job
- `src/Library.Api/BackgroundJobs/MemberSuspensionCronJob.cs` - `BackgroundService`, computes delay to next local midnight, suspends members with an overdue active borrow, self-disables via `FeatureFlags.EnableMemberSuspensionCronJob`, never crashes the host (logs faults to `runtime-error-logs` and keeps looping). `RunOnceAsync` is `internal` and callable directly for integration testing without waiting for real midnight.

### 2.5 Health check & log download
- `src/Library.Api/HealthChecks/PersistenceHealthCheck.cs` + `GET /health` (built-in ASP.NET Core health checks, no new package). Placeholder until a real DB exists (Phase 6) - contract won't need to change then.
- `src/Library.Api/Controllers/LogsController.cs` - `GET /api/logs/available?category=`, `GET /api/logs/download?category=&date=` (path-traversal guarded, filename convention shared with `FileAppLogWriter.GetFileTarget`).

### 2.6 Members lifecycle endpoints
- `POST /api/members/{id}/suspend|reactivate|renew` on `MembersController`, all relying on the central middleware for the 404 case (no local try/catch - intentional, this is the new pattern).

### 2.7 Configuration
- New `FeatureFlags` section in `src/Library.Api/appsettings.json`: `LogsRootPath`, `EnableRuntimeErrorLogging`, `EnableBuildErrorLogging`, `EnableQueryLogging`, `EnableExceptionLogging`, `EnableMemberSuspensionCronJob`, `EnableHealthCheckEndpoint`, `EnableLogDownloadEndpoint`. Bound once in `Program.cs` (composition root) into a plain `ObservabilitySettings` POCO shared as a DI singleton - deliberately **not** `IOptions<T>` to avoid any doubt about extra package references resolving correctly in an unverified build.

### 2.8 Interface / repository changes (breaking at the interface level - all call sites updated)
- `IMemberRepository`: added `UpdateAsync`, `GetAllAsync`.
- `IBorrowRecordRepository`: added `HasActiveBorrowAsync`, `GetOverdueActiveAsync`.
- `InMemoryMemberRepository`, `InMemoryBorrowRecordRepository`: implement the above.
- `AddInfrastructure(...)` in `src/Library.Infrastructure/DependencyInjection/InfrastructureServiceExtensions.cs` now takes `(ObservabilitySettings settings, string contentRootPath)` - **`Program.cs` call site was updated accordingly.**
- **Every hand-written test fake** implementing these interfaces was located by grep and updated: `MemberServiceTests.FakeMemberRepository`, `BorrowingServiceTests.FakeMemberRepository`, `BorrowingServiceTests.FakeBorrowRecordRepository`. Confirmed via `grep -rl "IMemberRepository\|IBorrowRecordRepository"` that no other implementers exist.

### 2.9 Tests added
- `MemberServiceTests`: `SuspendAsync` (success + not-found), `ReactivateAsync`, `RenewAsync`.
- `BorrowingServiceTests`: `IssueAsync_WhenMemberAlreadyHasActiveBorrow_ShouldThrow`.
- **Not attempted:** integration tests for the new middleware/cron/health/logs endpoints, load/stress/performance tests. Out of scope for an unverified single session - write these only after Section 4's build/test pass is confirmed green, so you're not debugging test-authoring mistakes and real compile errors at the same time.

## 3. Explicitly NOT started

- Bulk Excel import/export with row-level rollback and duplicate reporting.
- GitLab-style advanced search (multi-field, equals/not-equals/contains/excludes, independent sort-before/after-filter, ascending/descending) for Books, Members, and Book Copies.
- Full Members / Book Copies CRUD+search parity with Books.
- Frontend SweetAlert wiring for the generic error message (the backend message text exists in `GlobalExceptionHandlingMiddleware.SupportMessage`; no frontend interceptor has been written yet - `frontend/library-web/src/api/httpClient.ts` is still a bare axios instance).
- OpenTelemetry/Jaeger tracing.
- Dapper + EF Core + multi-RDBMS provider abstraction (MSSQL/Postgres/MySQL/Oracle/Access).
- `/api/release-notes/current` endpoint (release notes exist as markdown only).
- Any load/stress/performance testing.

## 4. Exact next commands

Run these first, in order, and report back exactly what fails:

```bash
cd LibraryManagementSystem
dotnet restore
dotnet build LibraryManagementSystem.slnx
dotnet test
```

If `dotnet build` fails, the most likely culprits (check these files
first, in this order):
1. `src/Library.Api/Program.cs` - most complex file touched this session (feature-flag binding, try/catch around `builder.Build()`/`app.Run()`, health-check mapping, local function at file scope in top-level statements).
2. `src/Library.Api/Middleware/GlobalExceptionHandlingMiddleware.cs` - pattern matching (`frame?.GetFileName() is { } f`) and nested anonymous objects in the Development-only JSON branch.
3. Any test fake under `tests/Library.UnitTests/Features/{Members,Borrowing}/` - interface member mismatches would surface here as `CS0535` (does not implement interface member).

If `dotnet test` fails on a test that already existed before this
session (not one of the new Suspend/Reactivate/Renew/one-borrow
tests), that's a regression - stop and diagnose before doing
anything else; it means the middleware or the enum-serialization
change altered existing behavior somewhere unexpected.

Once build+test are green, run the app and manually verify:
```bash
dotnet run --project src/Library.Api
# in another terminal:
curl http://localhost:5254/health
curl -X POST http://localhost:5254/api/members/{an-existing-id}/suspend
curl "http://localhost:5254/api/logs/available"
```
and confirm a `logs/` folder appears with dated files as members are
suspended/borrowed.

## 5. Recommended order after verification

1. Fix whatever `dotnet build`/`dotnet test` surfaces (see Section 4).
2. Wire the frontend SweetAlert fallback in `httpClient.ts` for the exact support message (small, high-value, closes the loop on this release's exception handling).
3. Then, per the ROADMAP's own gating, close the remaining Phase 1-5 MVP checklist items (search UX, dashboard, full Members/Copies CRUD) before starting bulk import / advanced search - those are large surfaces and benefit from a settled foundation.
4. Bulk Excel import + GitLab-style advanced search (the two features the user has flagged as the biggest remaining gap) should be tackled together since both touch the same `BookQuery`-style query model - consider designing one generic `AdvancedQuery` abstraction (field, operator, value; sort field, sort direction, sort-before-or-after-filter flag) shared across Books/Members/Copies rather than three bespoke implementations, to avoid the vertical-slice/CQRS over-engineering the user explicitly ruled out while still not repeating the same code three times.

## 6. Commit history for this session

See `git log` - one commit per logical unit (domain change, logging
infra, middleware, cron job, health/logs endpoints, tests, docs).
Nothing was squashed so the diff for any single piece can be reviewed
in isolation.
