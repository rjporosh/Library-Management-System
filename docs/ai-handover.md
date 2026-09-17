# AI Handover --- Library Management System

**Last updated:** 2026-09-17 (fifth checkpoint - JWT auth + RBAC foundation
for the member self-service / smart-library feature set)
**Written by:** Claude (principal-engineer role), in a sandbox **with a
working .NET 10 SDK, NuGet, Node 26 and Docker** - so unlike the 0.2.0
session, everything below is **built, tested and smoke-verified**.

Read this file first. `docs/ROADMAP.md` and `docs/MASTER_SPECIFICATION.md`
describe the long-term plan; this file is exactly where execution stands.

**If you are the next agent picking this up:** jump straight to
[§4f "Fifth checkpoint"](#4f-fifth-checkpoint-2026-09-17--auth-foundation)
and [§4g "Exact plan for the remaining smart-library feature set"](#4g-exact-plan-for-the-remaining-smart-library-feature-set-not-started-yet)
below - a large, multi-milestone feature set was requested and only the
auth foundation (milestone 1 of ~9) is done so far.

---

## 0. Current state (verified this session, 2026-09-17)

| Check | Result |
|---|---|
| `dotnet build LibraryManagementSystem.slnx` | **0 warnings, 0 errors** (TreatWarningsAsErrors on) |
| `dotnet test` | **86 pass** (57 unit + 29 integration), 0 fail |
| `npm test` (frontend, Vitest) | **11 pass** |
| `npm run build` / `npm run lint` (frontend) | clean |
| End-to-end auth flow (headless browser, real API + web, real Postgres) | librarian login -> dashboard -> logout -> member login -> `/books` with staff nav hidden; 0 console errors, 0 failed requests |
| EF Core migration `AddUsers` | generated + compiles; not yet applied to a long-lived dev DB (see §4f) |

Branch: `feat/enterprise-completion` (off `main`). Commits are one-per-milestone
with full messages. Two new commits this session: `bbc47ef` (backend auth) and
`927b075` (frontend auth UI) - see `git log` for the full messages.

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
dotnet test  LibraryManagementSystem.slnx      # expect 86 pass (57 unit + 29 integration)
( cd frontend/library-web && npm ci && npm test && npm run build && npm run lint )   # 11 tests, clean

# run the API - Development uses Postgres (docker compose up -d db first);
# for a no-database demo:  Database__Provider=InMemory dotnet run --project src/Library.Api
dotnet run --project src/Library.Api            # http://localhost:5254  (/scalar for docs)

# log in as the seeded demo accounts (see §4f) and reuse the token:
curl -X POST localhost:5254/api/auth/login -H 'content-type: application/json' \
  -d '{"usernameOrEmail":"librarian","password":"Librarian@123"}'
curl -X POST localhost:5254/api/auth/login -H 'content-type: application/json' \
  -d '{"usernameOrEmail":"alice@example.com","password":"Member@123"}'
TOKEN=<accessToken from above>
curl -X POST localhost:5254/api/books/search -H 'content-type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"filters":[{"field":"author","operator":"contains","value":"martin"}],"sort":[{"field":"publishedYear","direction":"desc"}]}'

# apply the new AddUsers migration to a real Postgres before relying on it outside InMemory:
docker compose up -d db
dotnet ef database update --project src/Library.Infrastructure --startup-project src/Library.Api

# full stack
docker compose up --build     # web :8080, api :5254, Jaeger :16686, db :5432

# frontend only
cd frontend/library-web && npm install && npm run dev   # http://localhost:5173

# NEXT STEP: this is NOT the end of the work. Read §4f and §4g above -
# a large "smart library" feature set was requested and only milestone 1
# (auth) is done. Continue with milestone 2 (book catalog enrichment:
# cover/edition/format + book detail page) next, in the exact order §4g
# lays out, committing one milestone at a time.
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

## 4d. Docker stack verified end-to-end (this session)

`docker compose up --build` was actually run: db (Postgres 17, healthy) +
jaeger + api + web all come up, `curl localhost:8080/api/books` through the
nginx proxy returns seeded data, `localhost:5254/health` is Healthy.

Fixed along the way:
- **`DatabaseBootstrapper` was wrong for a fresh DB** - it created the history
  table, then `HasTablesAsync()` saw *that* table and wrongly took the
  "adopt existing schema" branch, so `books` was never created and the seeder
  died with `42P01 relation "books" does not exist`. Rewritten to
  **try `MigrateAsync()` first and only baseline in the catch** when the error
  is "object already exists" (SqlState 42P07/42710). Correct for fresh,
  empty-history and stale-history databases.
- **`libgssapi_krb5.so.2` warning** on API startup (Npgsql Kerberos probe) -
  `src/Library.Api/Dockerfile` now installs `libgssapi-krb5-2`.
- Added `frontend/library-web/.dockerignore` (root `.dockerignore` doesn't
  apply to that build context) and a root **`README.md`** (quick start +
  doc index).

## 4e. Full Bengali UI + Scalar-in-Docker (this session)

- **Frontend i18n was shallow** - only API error messages were localised;
  all UI chrome stayed English. Now `src/lib/locales/{en,bn}.ts` +
  `t()` / `tStatus()` translate the entire interface (nav, page headers,
  dashboard tiles/cards, table headers, buttons, states, forms, advanced
  search, bulk-import dialog, toasts/confirms) and dates use `bn-BD`.
  `en.ts` is the source of truth; `bn.ts` is `Record<MessageKey,string>`
  so a missing key breaks the build; `i18n.test.ts` enforces parity.
  Verified in a browser against both the dev server and the docker web UI -
  EN and বাংলা render end to end, 0 console errors.
- **Scalar / OpenAPI were Development-only** so the dockerised (Production)
  API served neither. New `FeatureFlags:EnableApiReference` (default true);
  nginx also proxies `/scalar/` and `/openapi/`. Verified: `/scalar/v1`
  and `/openapi/v1.json` 200 through `http://localhost:8080` in compose.
- **Docker stack fully re-verified**: `docker compose up --build` ->
  db+jaeger+api+web healthy, `/api/books` POST persists to Postgres,
  Jaeger receives `Library.Api` traces, EN/BN UI both render.
  (A long red herring during debugging turned out to be a stray local
  `dotnet run` on port 5254 from the earlier browser QA - not a stack bug.)

Tests: 57 unit + 23 integration + 11 Vitest, all green; build 0/0.

## 4f. Fifth checkpoint (2026-09-17) - auth foundation

The user asked for a large "smart library" feature set on top of the already-
enterprise-ready MVP (see §4g for the full list and exact remaining plan).
Authentication was the correct place to start because several requested
features (member self-service, borrow requests, admin approval) are
impossible without a login/role system, and none existed before this session
- every endpoint was fully anonymous.

**What was built (backend):**
- `Library.Domain.Entities.User` (username, email, PBKDF2 password hash,
  `UserRole` enum `{Librarian, Member}`, optional `MemberId` link for member
  accounts, active flag, login timestamp). `Entity` base gives it soft-delete
  for free (unused for now, but consistent).
- `Pbkdf2PasswordHasher` (`Library.Application.Common.Security`) -
  RFC 2898/PBKDF2-HMACSHA256, 210k iterations, no external Identity package
  dependency; format `{iterations}.{saltB64}.{hashB64}` so the work factor
  can be raised later without invalidating existing hashes.
- `JwtTokenService` (`Library.Infrastructure.Security`) - HS256 via
  `System.IdentityModel.Tokens.Jwt`; claims: `sub`, name, email, `role`,
  `memberId` (when applicable), `jti`; expiry from `Jwt:AccessTokenMinutes`.
- `JwtOptions` bound from the `Jwt` config section (`Issuer`, `Audience`,
  `SigningKey`, `AccessTokenMinutes`). **`Program.cs` throws at startup if
  `Jwt:SigningKey` is empty** (fail-fast, same philosophy as the existing
  DB-down diagnostic) - so every environment's appsettings/env vars must set
  a real key. Dev/local/docker-compose already have placeholder keys
  (`*-CHANGE-ME-not-for-production-*`); **Production's `Jwt:SigningKey` is
  intentionally blank** - it must come from the `Jwt__SigningKey` environment
  variable (or a secrets manager) before a real deployment, same pattern as
  `Database:ConnectionString`.
- `AuthService` (`Library.Application.Features.Auth`) - `LoginAsync`,
  `RegisterMemberAsync` (creates the `Member` row AND the `User` login
  together, auto-generates a `MEM-<timestamp>` membership number),
  `RegisterLibrarianAsync` (librarian-only, for provisioning more staff).
  Uses the existing `Result`/`ApiError` pattern - duplicate email/username
  and weak-password errors all come back together, not one at a time.
- `AuthController`: `POST /api/auth/login` (anonymous), `POST /api/auth/
  register` (anonymous, member self-service), `POST /api/auth/librarians`
  (`[Authorize(Roles="Librarian")]`).
- **Every existing controller is now behind `[Authorize]`**: `BooksController`
  is `[Authorize]` at the class level (any signed-in role can browse/search/
  get) with `[Authorize(Roles="Librarian")]` added to Import/Create/Update/
  Delete individually. `BookCopiesController`, `MembersController`,
  `BorrowingController`, `DashboardController`, `JobsController`,
  `LogsController` are all `[Authorize(Roles="Librarian")]` at the class
  level (members have no reason to see any of those yet - member-facing
  borrow-request endpoints are still to be built, see §4g milestone 6).
  `MetadataController` and `ReleaseNotesController` were deliberately left
  anonymous (needed pre-login, e.g. for the login page's language switch).
- EF: `users` table (unique indexes on username/email/memberId), migration
  `20260917165936_AddUsers`. InMemory: `InMemoryUserRepository` mirroring the
  EF one, same DI-swap pattern as every other entity.
- Seed data (`SeedData.Build()`, shared by both providers) now also seeds a
  demo librarian (`librarian` / `Librarian@123`) and a member account linked
  to the existing seeded "Alice Johnson" member (`alice@example.com` /
  `Member@123`) - use these for manual testing and in any new automated test.

**What was built (frontend):**
- `AuthContext`/`authContextValue` (split into two files on purpose - a
  single file exporting both the provider component and the `useAuth` hook
  fails the `react-refresh/only-export-components` ESLint rule). Session
  persisted in `localStorage` under `lms.auth`.
- `src/lib/api.ts` - the axios instance now attaches `Authorization: Bearer
  <token>` from `localStorage` on every request, and a 401 anywhere clears
  the session and fires `window` event `lms:auth-logout` (`AuthContext`
  listens and updates state, so a token expiring mid-session correctly boots
  the user back to `/login`, not just the next request that happens to check).
- `LoginPage.tsx` / `RegisterPage.tsx` - plain, bilingual, follow the
  existing form conventions (`FormField`/`TextInput`/`Button` from
  `components/ui.tsx`, `normaliseError()` for error display).
- `App.tsx` - unauthenticated users only ever see `/login` and `/register`;
  once signed in, `nav` is filtered by `librarianOnly` per item (Dashboard,
  Book Copies, Members, Borrowing are hidden for a Member account), the `/`
  route resolves to the Dashboard for a Librarian and redirects a Member to
  `/books`, and a defensive `RequireLibrarian` wrapper still blocks direct
  URL navigation to a staff-only route even though the nav link is hidden.
  Sidebar shows "Signed in as `<username>`" + a logout button.
- New locale keys under `auth.*` and `nav.logout` added to **both** `en.ts`
  and `bn.ts` (the `Record<MessageKey,string>` typing means a missing key in
  `bn.ts` is a compile error, so parity is guaranteed here already).

**Testing:**
- `LibraryApiFactory` (integration test host) now mints matching test JWTs
  (`CreateLibrarianClient()` / `CreateMemberClient(memberId)`) instead of
  every test hitting anonymous endpoints. **Important gotcha discovered and
  fixed**: overriding `Jwt:*` config only via `ConfigureAppConfiguration` in
  the test factory was silently ignored for the minimal-hosting `Program.cs`
  pattern - it had to also go through `builder.UseSetting(...)` (exactly
  like the pre-existing `Database:Provider` override already did). If you
  add another appsettings key that a test factory needs to override, use
  `UseSetting`, not only `ConfigureAppConfiguration`.
- New `tests/Library.IntegrationTests/Features/Auth/AuthApiTests.cs` - login
  success/failure, member self-registration, anonymous-401, member-on-
  librarian-route-403, member-can-browse-books-200. Enum deserialization in
  tests needs `PropertyNameCaseInsensitive = true` in the test's
  `JsonSerializerOptions` (the API returns camelCase, `record` positional
  binding is case-sensitive by default) - without it every field silently
  binds to its default value instead of throwing, which is a sharp edge if
  you add more auth-shaped tests.
- Verified live in a headless browser against the real running stack
  (script and full transcript are not preserved, but the behavior asserted
  was: login redirect, nav visibility per role, default landing route per
  role, logout, re-login as a different role - all correct, 0 console
  errors, 0 failed network requests).

**Not done in this session (see §4g for the ordered remaining plan):** book
cover/thumbnail/edition/format fields, book-copy auto-generation, the 1-copy
borrow limit (still 1, not 2), member status filter UI, borrow-request/
approval workflow, voice search, the agentic chat assistant, and a
localization audit of the two new pages (LoginPage/RegisterPage are already
fully bilingual via `t()`, so this is really just "verify," not "build").

## 4g. Exact plan for the remaining smart-library feature set (not started yet)

The user's full ask (paraphrased, in the order it makes sense to build - each
is independent enough to be its own commit/milestone, and later ones depend
on earlier ones):

1. ~~**Auth (JWT, Librarian/Member RBAC)**~~ - **DONE, §4f above.**
2. **Book catalog enrichment**: `Book.CoverImageUrl` (nullable string - store
   as a URL/data-URI reference, do not build file upload/blob storage unless
   asked), `Book.Edition` (nullable, optional - show only if present),
   `Book.Format` (new enum `BookFormat { Physical, Ebook, AudioBook }` -
   **note**: a single physical book can plausibly also have an ebook/
   audiobook edition, so consider whether this should be a set of flags/
   separate boolean availability fields rather than a single enum before
   implementing - this is a real design decision, ask the user or pick the
   flags design, since a single enum cannot represent "available as both
   physical and ebook"), `Book.ExternalBuyUrl`/`Book.ExternalPdfUrl`
   (nullable - the "smart suggest a purchase/PDF link when nothing is
   available" fallback). Needs: domain field additions (mind the
   **positional-record landmine**, §5), EF migration, DTO/validator updates,
   a new **Book detail page** on the frontend (none exists today - Books is
   list-only; Member detail page at `src/pages/MemberDetailPage.tsx` is the
   template to copy for layout conventions), and "smart availability" logic
   (physical copy available -> show it; else ebook -> show it; else
   audiobook -> show it; else show the external buy/PDF link) most likely as
   a computed field on the book-detail response rather than client-side logic.
3. **Book-copy auto-generation on create**: add `Book.TotalCopies` (int,
   default 0) to the create-book request only (not persisted as a `Book`
   column necessarily - could be write-only, deriving the real count from
   `BookCopy` rows as today) or add it as a real column if the librarian
   needs to see/edit "intended total copies" separately from "copies that
   currently exist." When `TotalCopies` > 0 on create, generate that many
   `BookCopy` rows with **sequential barcodes** (`BC-0001`, `BC-0002`, ... -
   continuing from the current max `BC-####` in the DB, not restarting at
   0001 every time - check `BookCopyService`/`IBookCopyRepository` for how to
   query the current max cleanly, probably a new repository method). This
   must be one transaction (book + N copies all succeed or all fail) - use
   the existing `IUnitOfWork` pattern, do not call `SaveChangesAsync` per
   copy.
4. **Borrow limit 1 -> 2**: `src/Library.Application/Features/Borrowing/
   BorrowingService.cs` lines ~41-46 currently call
   `IBorrowRecordRepository.HasActiveBorrowAsync` (a boolean "has any active
   borrow"). Change this to a count-based check (`CountActiveBorrowsAsync`)
   against a configurable limit (suggest a `BorrowingOptions.MaxActiveBorrows
   = 2` in `Common/Options`, not a hardcoded literal, so it's a one-line
   config change later). Update the EF and InMemory repository
   implementations, the error message (currently says "Only one active
   borrow is allowed per member"), and **grep `tests/` for "Only one active
   borrow"** before changing - at least one existing test asserts that exact
   string and will need updating to match the new limit/message.
5. **Member list filters + borrow-count guard**: the member list/search
   already supports filtering by `status` (`MemberSearchMap` + the advanced
   search builder already whitelist it - verify field name in
   `Features/Members/MemberSearchMap.cs`) - this may already be usable from
   `MembersPage.tsx`'s `AdvancedSearch` component; confirm before building
   new UI. "Member who currently has a book borrowed" is already computable
   via `MemberDetailResponse.CurrentlyBorrowed` (see `MemberService.
   GetDetailAsync`) but is not currently a list-page filter/column - adding a
   "currently borrowing" column/filter to `MembersPage.tsx` would need either
   a new search field backed by a join/subquery in `EfMemberRepository.
   Query()`, or a denormalized flag - prefer the query-based approach to
   avoid a new sync-on-write bug.
6. **Borrow-request / admin-approval workflow** (net new - nothing like this
   exists today, confirmed by the initial exploration pass): new
   `BorrowRequest` entity (`MemberId`, `BookId` or `BookCopyId`, `Status`
   enum `{Pending, Approved, Rejected, Fulfilled}`, `RequestedAt`,
   `DecidedAt`, `DecidedByUserId`), a member-facing "request to borrow /
   request to buy" endpoint (`[Authorize(Roles="Member")]`), and a
   librarian-facing approval queue page + endpoints
   (`[Authorize(Roles="Librarian")]`) that, on approval, actually calls the
   existing `BorrowingService.IssueAsync` rather than duplicating that logic.
   Follow the existing feature-folder pattern (`Features/BorrowRequests/`
   with a `*Service.cs`, `Models/`, `*SearchMap.cs`) - see
   `docs/programmers-guide/02-add-a-crud.md` for the house style.
7. **Librarian search enhancements on the Borrowing page**: per the initial
   exploration, `BorrowingPage.tsx` already has type-ahead search by member
   name/membership number and by copy barcode - **re-verify this still
   satisfies "member name, member id, book name, copy id all visible and
   searchable"** before building anything new; likely only "book name" needs
   adding to the existing search/result columns (copies are currently
   searched/shown by barcode + book, not sure book title is a column - check
   the table columns in that page first).
8. **Localization audit**: per the initial exploration, UI-chrome
   localization was already completed in an earlier session (verified: grep
   of every `pages/*.tsx`/`components/*.tsx` for un-wrapped JSX text found
   none) - so "some pages still English" from the user's request may already
   be stale, OR may resurface once the new Book-detail/BorrowRequest pages
   are built (those must use `t()` from the start, not be built in English
   and translated after). Re-verify with a full-app click-through in both
   languages once milestones 2-7 land, since new pages are exactly where
   regressions happen.
9. **Voice search (STT) + agentic chat assistant** - user explicitly wants
   **both** provider tiers for each, all configurable via
   `appsettings.{json,Development.json,local.json}` or environment
   variables (matching the existing `Database`/`Jwt` config-driven-provider
   convention in this codebase):
   - **Speech**: Web Speech API (`SpeechRecognition`/`speechSynthesis`,
     client-side, zero backend work, Chromium-only) as the default/
     recommended path, **plus** a Hugging Face-backed server path (e.g.
     Whisper via Inference API or a self-hosted endpoint) as a configurable
     alternative - suggest a `Speech:Provider` config value (`WebSpeech` |
     `HuggingFace`) with `Speech:HuggingFace:ApiKey`/`ModelId` sub-keys,
     following the exact `DatabaseOptions`/`JwtOptions` POCO-bound-once
     pattern already used everywhere else in this codebase. **Needs a
     `HUGGINGFACE_API_KEY` the user must supply** - do not fabricate one.
   - **Chat assistant**: a rule-based deterministic intent engine (parse
     "how many copies of X", "most borrowed this month", "who borrowed the
     most last month", etc. against the existing repositories/dashboard
     queries - no new external dependency, always available) as the
     default/fallback, **plus** a real LLM path supporting **both**
     `ANTHROPIC_API_KEY` and `OPENAI_API_KEY` (user explicitly asked for
     both, said they have a trainer-provided OpenAI key to test with),
     selectable via a `Chat:Provider` config value (`RuleBased` |
     `Anthropic` | `OpenAI`), with the LLM path constrained to call the same
     internal query "tools" (function-calling / tool-use) rather than given
     free-form DB access - this is a real architectural decision (external
     cost, latency, a new outbound network dependency) and was explicitly
     confirmed with the user (see the two `AskUserQuestion` answers in the
     conversation that requested this milestone) before starting; if a fresh
     agent is continuing here without that context, it is safe to proceed
     with this design as specified.
   - Both must be documented in `guide.md` (the user explicitly asked for
     "an easy step by step guide" there covering how to configure each
     provider and how to switch between them) - update `guide.md` in the
     same commit that adds the feature, not as an afterthought.
10. **Everything above** must land with 0 build warnings/errors (the
    `TreatWarningsAsErrors` gate already enforces this), no regressions in
    the current 86 backend + 11 frontend tests, and a docs/roadmap/release-
    notes update + commit **per milestone**, exactly like milestones 1
    (backend auth) and 1b (frontend auth UI) in §4f were landed this
    session - do not batch multiple milestones into one commit, and do not
    leave a milestone half-done across a context/session boundary without
    updating this file to say exactly which half is done.

**If your context/token budget is running out before finishing a milestone**:
stop at the next safe point (usually: after the backend for a milestone
builds + tests pass, before starting its frontend half, or vice versa),
commit what compiles and is tested, and add a `## 4h. <n>th checkpoint`
section here describing exactly what changed, why, and the precise resume
point - the same structure §4f uses. Do not leave partially-applied EF
migrations, half-added positional-record fields, or TODO/stub code across a
checkpoint boundary.

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
- **Every controller except `AuthController`, `MetadataController` and
  `ReleaseNotesController` now requires a bearer token** (see §4f). Any new
  integration test that calls a controller endpoint needs
  `factory.CreateLibrarianClient()` or `factory.CreateMemberClient(memberId)`
  instead of `factory.CreateClient()`, or it will get a 401. Any new
  frontend API call goes through the shared `http` instance in `lib/api.ts`
  and gets the bearer token automatically - do not build a second axios
  instance.
- `Jwt:SigningKey` **must** be non-empty in every environment's config or
  the API refuses to start (by design, fail-fast). If you add a new test
  host or a new deployment environment file, give it a `Jwt:SigningKey` too.
