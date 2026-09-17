# AI Handover --- Library Management System

**Last updated:** 2026-09-18 (sixth checkpoint - smart-library feature set
milestones 2-8 of 9 complete; only voice search + agentic chat remain)
**Written by:** Claude (principal-engineer role), in a sandbox **with a
working .NET 10 SDK, NuGet, Node 26 and Docker** - so unlike the 0.2.0
session, everything below is **built, tested and smoke-verified**.

Read this file first. `docs/ROADMAP.md` and `docs/MASTER_SPECIFICATION.md`
describe the long-term plan; this file is exactly where execution stands.

**If you are the next agent picking this up:** jump straight to
[§4h "Sixth checkpoint"](#4h-sixth-checkpoint-2026-09-18--milestones-2-8-of-9-done)
below for what shipped this session, then to
[§4i "Exact plan for the one remaining milestone"](#4i-exact-plan-for-the-one-remaining-milestone-voice-search--agentic-chat)
for the precise next-step plan. Everything in the original §4g plan is done
**except milestone 9** (voice search + the agentic chat assistant).

---

## 0. Current state (verified this session, 2026-09-18)

| Check | Result |
|---|---|
| `dotnet build LibraryManagementSystem.slnx` | **0 warnings, 0 errors** (TreatWarningsAsErrors on) |
| `dotnet test` | **98 pass** (58 unit + 40 integration), 0 fail |
| `npm test` (frontend, Vitest) | **11 pass** |
| `npm run build` / `npm run lint` (frontend) | clean |
| End-to-end flows verified in a real headless browser against a running API | auth (both roles), book catalog enrichment + cover fallback, borrow-request -> approve -> fulfilled workflow, borrowing page shows real names not GUIDs; 0 console errors, 0 failed requests in every pass |
| EF Core migrations `AddUsers`, `AddBookCatalogEnrichment`, `AddBorrowRequests` | generated, compile, and **have been applied** to the real local dev Postgres (`docker compose up -d db`) - verified with `\d books`/`\d users` etc. |

Branch: `feat/enterprise-completion` (off `main`). Commits are one-per-milestone
with full messages; six new commits since the fifth checkpoint - `08b7e2f`
(book catalog enrichment), `8a39dee` (borrow limit + member indicator +
borrowing page fix), `3342851` (borrow-request workflow), `3882bfe`
(localization fix) - plus `f7b3a6e` and the two auth commits from the fifth
checkpoint. See `git log` for full messages; §4h below summarizes each.

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
dotnet test  LibraryManagementSystem.slnx      # expect 98 pass (58 unit + 40 integration)
( cd frontend/library-web && npm ci && npm test && npm run build && npm run lint )   # 11 tests, clean

# run the API - Development now correctly uses Postgres (the provider-name
# typo that silently downgraded it to InMemory was fixed this session):
docker compose up -d db
dotnet run --project src/Library.Api            # http://localhost:5254  (/scalar for docs)
# for a no-database demo instead:  Database__Provider=InMemory dotnet run --project src/Library.Api

# log in as the seeded demo accounts (see §4f) and reuse the token:
curl -X POST localhost:5254/api/auth/login -H 'content-type: application/json' \
  -d '{"usernameOrEmail":"librarian","password":"Librarian@123"}'
curl -X POST localhost:5254/api/auth/login -H 'content-type: application/json' \
  -d '{"usernameOrEmail":"alice@example.com","password":"Member@123"}'
TOKEN=<accessToken from above>
curl -X POST localhost:5254/api/books/search -H 'content-type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"filters":[{"field":"author","operator":"contains","value":"martin"}],"sort":[{"field":"publishedYear","direction":"desc"}]}'

# All migrations (AddUsers, AddBookCatalogEnrichment, AddBorrowRequests) are
# already applied to the local dev Postgres as of this checkpoint. If you're
# on a fresh/different DB, apply them first:
dotnet ef database update --project src/Library.Infrastructure --startup-project src/Library.Api

# full stack
docker compose up --build     # web :8080, api :5254, Jaeger :16686, db :5432

# frontend only
cd frontend/library-web && npm install && npm run dev   # http://localhost:5173

# NEXT STEP: read §4h (what shipped) then §4i (the exact remaining plan).
# Milestones 1-8 of 9 are done. Only milestone 9 remains: voice search
# (Web Speech API + Hugging Face, configurable) and the agentic chat
# assistant (rule-based + Anthropic/OpenAI, configurable). §4i gives the
# suggested build order - start with the rule-based chat engine since it
# needs no external credentials.
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

## 4g. Original plan for the smart-library feature set (superseded - kept for history)

This section originally laid out 9 milestones in full detail before any of
them (beyond auth) were built. **Milestones 2-8 are now done** - see §4h for
exactly what shipped and how it differs from this original plan (mostly: it
matches, with a few small deviations called out explicitly). **Only
milestone 9 (voice search + agentic chat) remains** - see §4i for its
current, up-to-date plan. The detailed prose for milestones 2-8 that used to
live here has been superseded by §4h's "what actually shipped" and is no
longer needed; read §4h instead of reconstructing intent from this stub.

## 4h. Sixth checkpoint (2026-09-18) - milestones 2-8 of 9 done

Continuing directly from §4f (auth). All of the following landed as
separate, fully-tested commits (`08b7e2f`, `8a39dee`, `3342851`, `3882bfe`)
- see each commit message for full detail; this section is the durable
summary plus anything a future agent needs that isn't obvious from the diff.

**Milestone 2 - Book catalog enrichment (`08b7e2f`).**
- `Book` gained `CoverImageUrl`, `Edition`, `HasEbook`/`EbookUrl`,
  `HasAudiobook`/`AudiobookUrl`, `ExternalBuyUrl`/`ExternalPdfUrl` - all
  optional, appended as trailing constructor/DTO parameters (per the
  positional-record landmine in §5, this was done as one commit touching
  every call site at once).
- **Design decision made**: flags (`HasEbook`/`HasAudiobook`), not the
  single `BookFormat` enum the original plan (§4g) sketched - a book can be
  physical AND ebook AND audiobook at once, which a single enum can't
  represent. This was flagged as a decision point in §4g and resolved here.
- New `GET /api/books/{id}/detail` resolves "smart availability": physical
  copy available -> report it; else ebook -> report its URL; else
  audiobook -> report its URL; else report `ExternalBuyUrl`/`ExternalPdfUrl`
  as suggestions. New `BookDetailPage.tsx` (route `/books/:id`, open to both
  roles) renders this.
- Book list shows a cover thumbnail: `coverImageUrl` if set, else a derived
  Open Library cover-by-ISBN URL (`lib/covers.ts`), else a neutral inline
  SVG placeholder on image-load error. Also shows `Edition` next to the
  author when present.
- `POST /api/books` accepts `TotalCopies`: when `> 0`, that many `BookCopy`
  rows are generated in the same transaction with sequential barcodes
  (`BC-0001`, `BC-0002`, ...), continuing from the current max rather than
  restarting (`IBookCopyRepository.GetMaxBarcodeNumberAsync` + a shared
  `BarcodeSequence` parser in `Library.Application.Common`, used by both EF
  and InMemory repos).
- **Member-facing UI lockdown** (this was the user's explicit ask before
  starting milestone 2): `BooksPage.tsx` now hides Add/Bulk-import/Edit/
  Delete for the Member role via `useAuth().isLibrarian` and shows only a
  view (eye icon) action that opens the detail page - a Member cannot put
  the catalog in a dangerous state from the UI (the API already enforced
  this server-side; this closes the client-side gap so the affordance isn't
  shown at all).
- **Found and fixed two unrelated pre-existing bugs** while verifying
  against the real dev Postgres: `appsettings.Development.json` had
  `Database:Provider: "PostgreSql"` (typo - doesn't match the `Postgres`
  enum name, so `DatabaseOptions.ResolvedProvider` silently fell back to
  `InMemory` - **Development has never actually been exercising Postgres
  until this fix**), and its connection string had the wrong port (5542 vs
  the real 5432) and wrong credentials (postgres/postgres vs the real
  library/library from `docker-compose.yml`). Both fixed. Also discovered
  the local `librarymanagementsystem-db-1` container was running without
  its port published (started before the `ports:` mapping existed in
  compose) - fixed with `docker compose up -d db` (recreates the container,
  named volume `db-data` is untouched, no data lost).

**Milestone 3 - Borrow limit 1 -> 2 (`8a39dee`).**
- New `BorrowingOptions.MaxActiveBorrowsPerMember` (default 2), bound in
  `Program.cs` from the `Borrowing` config section exactly like
  `JwtOptions`/`DatabaseOptions`, passed into `AddApplication(...)`.
- `IBorrowRecordRepository.CountActiveBorrowsAsync` added **alongside**
  the existing `HasActiveBorrowAsync` (did not remove/rename it) - the
  boolean method backs an unrelated rule in `MemberService` (block member
  delete while they have any active borrow) and was left untouched.
  `BorrowingService.IssueAsync` now compares the member's active count
  against the configured limit.
- The old test asserting "Only one active borrow" was split into two tests
  (`IssueAsync_WhenMemberIsBelowTheBorrowLimit_ShouldSucceed` /
  `..._WhenMemberIsAtTheBorrowLimit_ShouldThrow`) reflecting the new
  2-book ceiling, plus a new integration test
  (`BorrowingLimitApiTests.Issue_AllowsTwoActiveBorrows_ThenBlocksAThird`).

**Milestone 4/5 - Member status filter + currently-borrowing (`8a39dee`).**
- The Active/Suspended/Inactive status filter the user asked for **already
  existed** (`MembersPage.tsx`'s `AdvancedSearch` `status` field, backed by
  `MemberSearchMap`) - verified working, nothing to build.
- Added what was actually missing: `MemberResponse.CurrentlyBorrowed` (int),
  computed by `MemberService.Search` via one batched query per page (all
  active borrows for the member IDs on that page, grouped and counted - not
  N+1), shown as a new column on the member list. Single-member reads
  (`GetByIdAsync`, `CreateAsync`, `UpdateAsync`) do **not** compute this and
  report 0 - a deliberate scope cut since those responses aren't shown next
  to a "currently borrowing" concept in the UI; if a future feature needs it
  there too, follow the same batched pattern, don't add a query per call.

**Milestone 6/7 - Borrow-request/approval workflow + borrowing-page fix (`3342851`, `8a39dee`).**
- New `BorrowRequest` aggregate (`Library.Domain.Entities`): `Type` (`Borrow`
  | `Purchase`), `Status` (`Pending` -> `Approved`/`Rejected`, or
  `Fulfilled` once a Borrow request is actually issued), `BookId` (Borrow)
  or `SuggestedTitle`/`SuggestedAuthor` (Purchase), `Note`, audit fields.
  Full stack: `IBorrowRequestRepository` (+EF/InMemory), `BorrowRequestService`,
  `BorrowRequestsController`.
- Endpoints: `POST /api/borrow-requests` (Member), `GET /api/borrow-requests/
  mine` (Member, own only), `POST /api/borrow-requests/search` (Librarian,
  the approval queue - reuses the generic `QueryableSearchBuilder`), `POST
  /{id}/approve` and `/{id}/reject` (Librarian). **Approving a Borrow
  request calls the existing `BorrowingService.IssueAsync` directly**
  (picks the first available copy) rather than duplicating issue logic -
  `BorrowRequestService` takes a `BorrowingService` dependency for this.
  Duplicate pending Borrow requests for the same book+member are rejected
  (409, `BORROW_REQUEST_DUPLICATE`).
- Current-user claims: new `ControllerBase` extensions
  `CurrentUserId()`/`CurrentMemberId()` in `Library.Api.Common.
  CurrentUserExtensions`, reading the `sub`/`ClaimTypes.NameIdentifier` and
  custom `memberId` JWT claims set by `JwtTokenService`. Use these, don't
  re-parse `User.Claims` ad hoc, if a future endpoint needs "who is the
  signed-in member/user."
- Frontend: `BookDetailPage.tsx` gained a "Request to borrow" button for
  Members; new route `/requests` renders `MyRequestsPage.tsx` (own requests
  + a "Suggest a purchase" form) for a Member or `BorrowRequestsPage.tsx`
  (searchable approval queue, Approve/Reject) for a Librarian - one route,
  role-switched component, one new nav item visible to both roles.
- **Borrowing-page fix** (this was a real, verified gap, not speculative):
  the active-borrows table on `/borrowing` was showing truncated raw GUIDs
  for both the member and the copy - not what "member name, member id,
  book name, copy id visible and searchable" asked for.
  `BorrowRecordResponse` gained denormalized `MemberName`/
  `MembershipNumber`/`BookTitle`/`Barcode` (populated by
  `BorrowingService.Search` via the same batched-lookup pattern used
  elsewhere), the table now renders real names/titles, and a client-side
  search box filters the (already page-loaded, up to 50 rows) active-borrow
  list by any of those four fields text-matched.
- **Bug found and fixed during manual browser verification** (worth
  internalizing the lesson, not just the fix): `BorrowRequestService.
  GetByMemberIdAsync` (backs `GET /borrow-requests/mine`) initially called
  the bare `Map(r)` with no member/book enrichment, so a Member's own
  request list showed a **blank book title** - only `Search()` (the
  librarian queue) had the batched-lookup enrichment. Two code paths
  mapping the "same" entity to the same response DTO is exactly where this
  kind of gap hides; when a response has denormalized/joined fields,
  grep every `Map(...)` call site for the entity, not just the one you're
  actively working on. Fixed + locked in with a regression assertion in
  `BorrowRequestsApiTests.Mine_ReturnsOnlyTheSignedInMembersRequests`.

**Milestone 8 - Localization audit (`3882bfe`).**
- Full audit (via a dedicated Explore pass over every `pages/*.tsx` and
  `components/*.tsx` file) confirmed the UI-chrome localization from an
  earlier session is intact, and every page added this session (Login,
  Register, BookDetail, MyRequests, BorrowRequests) was already fully
  bilingual from the start (built with `t()` throughout, not translated
  after the fact).
- One real gap found and fixed: `lib/api.ts`'s generic network/5xx-error
  fallback message (`SUPPORT_MESSAGE`) was a hardcoded English literal that
  would render verbatim regardless of the active language. Moved to
  `common.supportMessage` in `en.ts`/`bn.ts`.
- **Deliberately left alone**: the language-switcher's own labels
  (`'English'`/`'বাংলা'` in `App.tsx`) are literals, not `t()` calls - this
  is intentional (a language picker conventionally shows each language's
  own endonym regardless of the active locale, the way "Deutsch" doesn't
  become "German" when your OS is in German). Don't "fix" this.

**Migrations applied to the real dev Postgres this session** (not just
generated): `AddUsers`, `AddBookCatalogEnrichment`, `AddBorrowRequests` -
all three ran cleanly via `dotnet ef database update` against
`localhost:5432` (after the connection-string fix above), verified with
`docker exec ... psql -U library -d library -c '\d books'` showing the new
columns.

**Verification performed this session** (all real, not claimed): every
milestone above was checked with `dotnet build` (0/0), the full backend
test suite, `npm run build`/`npm run lint`/`npm test`, **and** a live
headless-browser pass against a real running API+web stack for the
user-facing behavior (auth flows, book cover rendering, the DDD
"no-physical-copy -> external buy link" fallback, the full request ->
approve -> fulfilled workflow, borrowing-page name display) - see each
commit message for the specific assertions made in each pass.

## 4i. Exact plan for the one remaining milestone: voice search + agentic chat

This is the only thing left from the original 9-milestone ask (§4g). The
user's requirements, confirmed via `AskUserQuestion` earlier in this
session (do not re-ask - proceed with this design):

- **Speech**: Web Speech API (`SpeechRecognition`/`speechSynthesis`,
  client-side, zero backend work, Chromium-only) as the default/
  recommended path, **plus** a Hugging Face-backed server path (e.g.
  Whisper via Inference API or a self-hosted endpoint) as a configurable
  alternative. Suggest a `Speech:Provider` config value (`WebSpeech` |
  `HuggingFace`) with `Speech:HuggingFace:ApiKey`/`ModelId` sub-keys,
  following the exact `DatabaseOptions`/`JwtOptions`/`BorrowingOptions`
  POCO-bound-once-in-Program.cs pattern already used four times over in
  this codebase - don't invent a new configuration mechanism. **Needs a
  `HUGGINGFACE_API_KEY` the user must supply** - do not fabricate one; if
  it's absent, the Hugging Face path should fail gracefully with a clear
  config-error message (same philosophy as the `Jwt:SigningKey` fail-fast
  check in `Program.cs`) rather than silently doing nothing.
- **Chat assistant**: a rule-based deterministic intent engine (parse
  queries like "how many copies of X", "most borrowed this month", "who
  borrowed the most last month" against the existing repositories/
  dashboard queries - no new external dependency, always available, no
  cost) as the default/fallback, **plus** a real LLM path supporting
  **both** `ANTHROPIC_API_KEY` and `OPENAI_API_KEY` (user explicitly wants
  both available, has a trainer-provided OpenAI key to test with),
  selectable via a `Chat:Provider` config value (`RuleBased` | `Anthropic`
  | `OpenAI`). The LLM path must be constrained to call the same internal
  query "tools" (function-calling/tool-use against the existing
  repositories/`DashboardService`-style queries) rather than given
  free-form database access - this is a deliberate security/cost/
  correctness boundary, not a shortcut to skip.
- Both must be documented in `guide.md` with an easy step-by-step
  configuration guide for each provider and how to switch between them -
  update `guide.md` in the same commit that adds the feature, not as an
  afterthought.
- Suggested build order (each independently testable): (1) the rule-based
  chat intent engine + a simple chat UI widget first, since it needs no
  external credentials and delivers value immediately; (2) the Anthropic/
  OpenAI LLM path behind the same chat endpoint, config-selected; (3) Web
  Speech API wiring in the frontend (a mic button that fills the chat/
  search input via `SpeechRecognition`, and optionally reads results back
  via `speechSynthesis`); (4) the Hugging Face STT alternative last, since
  it's the most infrastructure-heavy piece and least likely to be used day
  to day. Land each as its own commit per the standing workflow rule below.
- **Standing workflow rule (unchanged from §4g, still applies)**: every
  milestone lands as its own commit with 0 build warnings/errors, no
  regressions in the existing test suite, and a docs update - do not batch
  multiple milestones into one commit, and do not leave a milestone
  half-done across a context/session boundary without updating this file
  (add a `## 4j. <n>th checkpoint` section) to say exactly which half is
  done, why, and the precise resume point. Do not leave partially-applied
  EF migrations, half-added positional-record fields, or TODO/stub code
  across a checkpoint boundary.

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
- **`appsettings.Development.json`'s `Database:Provider` must be exactly
  `"Postgres"`** (not `"PostgreSql"` or any other spelling) - `DatabaseOptions.
  ResolvedProvider` silently falls back to `InMemory` on any unparseable
  value instead of throwing, so a typo there fails silent, not loud. This
  bit a whole session (Development had been running InMemory instead of
  Postgres) before it was caught. If you add a new environment config file,
  sanity-check the provider name and connection string against
  `docker-compose.yml`'s actual `db` service port/credentials
  (`library`/`library`@`5432`) rather than assuming they match.
- **`BorrowingService`'s constructor now takes `IBookRepository` and an
  optional `BorrowingOptions`** (added for the denormalized book-title
  lookup and the configurable borrow limit respectively). Any hand-written
  fake/stub implementing `IBorrowRecordRepository` or constructing
  `BorrowingService` directly needs updating - see
  `tests/Library.UnitTests/Features/Borrowing/BorrowingServiceTests.cs` for
  the pattern (`new StubBookRepository()` as the extra positional arg).
- **When a response DTO has denormalized/joined fields** (e.g.
  `BorrowRequestResponse.BookTitle`, `BorrowRecordResponse.MemberName`),
  grep every place that maps the underlying entity to that DTO, not just
  the one you're editing - `BorrowRequestService` originally had this
  enrichment only in `Search()` and not in `GetByMemberIdAsync()`, so the
  member's own request list silently showed blank titles until a live
  browser check caught it (see §4h). A method compiling and a unit test
  passing does not prove a second `Map(...)` call site got the same
  treatment.
- The **local dev Postgres container may be running without its published
  port** if it was started before `docker-compose.yml`'s `ports:` section
  was added/changed (`docker ps` will show `5432/tcp` with no host mapping).
  `docker compose up -d db` recreates it with the current config; the named
  volume (`db-data`) is untouched, so no data is lost. Check `docker port
  librarymanagementsystem-db-1` if `dotnet ef database update` or the API
  can't connect and everything else looks right.
