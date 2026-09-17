# ROADMAP --- Library Management System

**Strategy:** Complete the MVP first, verify it rigorously, then evolve
it into an enterprise-grade product.

------------------------------------------------------------------------

## Progress snapshot (2026-09-17, branch `feat/enterprise-completion`)

**New:** Phase 17 (Smart Library feature set - auth, book formats/covers,
copy auto-generation, borrow requests, voice search, agentic chat) was added
2026-09-17. Only 17.1 (Auth/RBAC) is done; see `docs/ai-handover.md` §4g for
the full ordered plan for 17.2-17.9.

| Phase | Status |
|---|---|
| 1 MVP functional completion | **Done** (Books/Members/Copies/Borrowing full CRUD + search + dashboard) |
| 2 GitLab-style multi-select search | **Done** - generic expression-tree builder, all four resources, enum-by-name |
| 3 UX + validation + error contract | **Done** - Result pattern, ErrorCodes, 422 with full list, SweetAlert2 wired |
| 4 Dashboard aggregates | **Done** - `GET /api/dashboard`, no N+1 |
| 5 MVP test gate | **Passed** - 60 tests, 0 warnings, e2e browser check |
| 9 Result pattern + central error handling | **Done** (new features); old controllers migrated opportunistically |
| 10 Logging | file logging + correlation id done; **OpenTelemetry pending** |
| 12 Excel bulk import | **Done** - all-or-nothing, all §21 scenarios |
| 6 EF Core / DB foundation | **Done** - EF Core, PostgreSQL primary, provider factory, unit of work, migration, DB-down diagnostics, real health check, EF seeder |
| 10 Observability (OpenTelemetry) | **Done** - OTLP traces + metrics to Jaeger, appsettings-toggled |
| 11 EF Core + Dapper split | **Done** - EF for writes/search; Dapper for the dashboard aggregate (`Orm=Dapper`, Postgres/SQLite), parity-tested |
| 13 Security & resilience | **Done** - per-client rate limiting, forwarded headers, RFC 7807 problem+json, localization (en/bn) |
| 14 Docs (ADR/C4/SQL) | **Done** - guide.md, MIGRATIONS.md, programmer's guide (13 files), 7 ADRs, C4 + ER (Mermaid), schema.sql + seed-data.sql |
| 15 Release Notes API | **Done** - `GET /api/release-notes/current` |
| 16 Release engineering / Docker / CI | **Done** - multi-stage Dockerfiles, docker-compose (db+jaeger+api+web), GitHub Actions CI (+ frontend Vitest), NBomber load tests |
| 8 Domain integrity / soft delete | **Done** - Entity base, soft delete everywhere, smart cascade-delete, Book category/publisher + Member phone/address |
| 17.1 Auth (JWT, Librarian/Member RBAC) | **Done** - see `docs/ai-handover.md` §4f |
| 17.2 Book catalog enrichment (cover/edition/format, book detail page) | **Not started** |
| 17.3 Book-copy auto-generation on create (`TotalCopies` -> `BC-####` rows) | **Not started** |
| 17.4 Borrow limit 1 -> 2 (configurable) | **Not started** |
| 17.5 Member list status filters / currently-borrowing indicator | **Not started** (may partly already work - verify first) |
| 17.6 Borrow-request / admin-approval workflow | **Not started** |
| 17.7 Borrowing page search verification (member/book/copy) | **Not started** (verify existing search first) |
| 17.8 Localization audit of new pages | **Not started** |
| 17.9 Voice search (Web Speech + Hugging Face) + agentic chat (rule-based + Anthropic/OpenAI) | **Not started** |

See `docs/ai-handover.md` §3-4 and §4f-4g for the exact next steps and commands.

------------------------------------------------------------------------

## Phase 0 --- Baseline Audit

### Goals

-   Review original requirements.
-   Compare requirements with current repository.
-   Identify missing functionality and contract mismatches.
-   Keep original requirement PDF attached in `docs/`.

### Exit criteria

-   Requirement-to-feature matrix exists.
-   All missing items are tracked.
-   No requirement is silently dropped.

------------------------------------------------------------------------

# Phase 1 --- MVP Functional Completion

## 1.1 Frontend build

Fix all TypeScript/Vite build errors.

Current known issues to eliminate:

-   unused `queryClient0`
-   unused `Mail`
-   unused `User`
-   unused `toggleSortField`

### Exit criteria

``` bash
npm run build
```

passes with zero TypeScript/build errors.

------------------------------------------------------------------------

## 1.2 Domain completion

Bring domain/API/UI into alignment.

### Book

Ensure:

-   title
-   author
-   ISBN
-   category
-   publisher
-   description where applicable
-   published year

### Member

Ensure:

-   member ID
-   membership number
-   name
-   phone
-   address
-   status

### Book Copy

Ensure:

-   copy ID
-   book relationship
-   barcode/copy identifier
-   status

### Borrow

Ensure:

-   member
-   copy
-   issue date
-   due date
-   return date
-   status

------------------------------------------------------------------------

## 1.3 Book management

Complete:

-   list
-   create
-   update
-   details
-   validation
-   availability
-   copy display
-   borrower/due-date information where applicable

------------------------------------------------------------------------

## 1.4 Member management

Complete:

-   list
-   create/update
-   search
-   details
-   current borrowings
-   history
-   borrowing summary
-   overdue information

------------------------------------------------------------------------

## 1.5 Issue workflow

Implement the intended librarian workflow:

``` text
Member ID / Phone
      ↓
Member result
      ↓
Available copies
      ↓
Due date
      ↓
Confirmation
      ↓
Issue
      ↓
Success alert
```

Prevent:

-   inactive/suspended member borrowing
-   unavailable copy borrowing
-   duplicate active borrowing

------------------------------------------------------------------------

## 1.6 Return workflow

Implement:

``` text
Search active borrowing
      ↓
Review member/book/copy
      ↓
Confirm
      ↓
Return
      ↓
Copy becomes Available
      ↓
Success alert
```

Prevent double return.

------------------------------------------------------------------------

# Phase 2 --- Search Upgrade

## 2.1 GitLab-style field multi-select

Provide:

``` text
Search in:
☑ Title
☑ Author
☐ ISBN
```

Requirements:

-   one or many fields
-   default Title when none selected
-   OR semantics across selected fields
-   case-insensitive
-   trimmed
-   contains search
-   multiple values where exposed by the UI
-   pagination compatible
-   sorting compatible

## 2.2 API consistency

Frontend options must exactly match backend-supported search fields.

Remove unsupported UI options.

## 2.3 Search tests

Test:

-   title only
-   author only
-   ISBN only
-   title + author
-   title + ISBN
-   all fields
-   no selected field
-   empty query
-   case-insensitivity
-   whitespace
-   pagination
-   sorting

------------------------------------------------------------------------

# Phase 3 --- UX and Validation

## 3.1 SweetAlert2

Implement:

-   success alerts
-   error alerts
-   confirmation dialogs
-   destructive-action confirmation
-   bulk import result dialogs
-   loading/duplicate-submit protection

## 3.2 Error contract

Standardize:

``` json
{
  "success": false,
  "errors": [
    {
      "errorCode": "...",
      "errorMessage": "...",
      "field": "...",
      "line": 14,
      "required": true,
      "supportedValues": "..."
    }
  ]
}
```

`line` is included when the error originates from a row-based source
such as Excel.

## 3.3 Static error resources

Centralize:

-   error codes
-   messages
-   field names
-   required flags
-   supported values/constraints

------------------------------------------------------------------------

# Phase 4 --- Dashboard and Reporting

Complete live dashboard:

-   total books
-   total copies
-   available copies
-   borrowed copies
-   members
-   recent borrowing

Avoid N+1 API patterns.

Create efficient aggregate endpoints where needed.

------------------------------------------------------------------------

# Phase 5 --- MVP Test Gate

Add/fix:

-   unit tests
-   integration tests
-   frontend build verification
-   search regression tests
-   issue/return tests
-   validation tests

### Mandatory gate

No enterprise architecture begins until:

-   build passes
-   tests pass
-   core workflows pass
-   search passes
-   SweetAlert passes
-   requirements checklist passes

------------------------------------------------------------------------

# Phase 6 --- Database Foundation

Move from in-memory persistence to relational persistence.

Deliver:

-   database schema
-   EF Core configuration
-   migrations
-   indexes
-   unique constraints
-   foreign keys
-   seed data
-   schema SQL
-   seed SQL
-   ER diagram

Design provider abstraction carefully for supported SQL databases.

------------------------------------------------------------------------

# Phase 7 --- Enterprise Architecture

Introduce deliberately:

## 7.1 Vertical Slice Architecture

Organize by feature/use case:

``` text
Features/
  Books/
  BookCopies/
  Members/
  Borrowing/
  BulkImport/
  ReleaseNotes/
```

## 7.2 CQRS

Separate:

-   commands
-   queries
-   handlers

Use CQRS where it provides clarity and independent read/write behavior.

## 7.3 MediatR

Introduce MediatR as the application dispatch/pipeline mechanism where
appropriate.

Add behaviors for:

-   validation
-   logging
-   performance
-   transactions where justified

------------------------------------------------------------------------

# Phase 8 --- Domain Integrity

Implement strong domain invariants.

Examples:

``` text
Available copy
    → cannot have active borrow

Borrowed copy
    → must have active borrow

Returned borrow
    → cannot be returned again

Suspended member
    → cannot issue

Duplicate ISBN/copy identifier
    → rejected
```

The system must make invalid state difficult or impossible to represent.

------------------------------------------------------------------------

# Phase 9 --- Result Pattern and Error Handling

Implement a consistent Result/error model.

Use Result for expected business/application outcomes where useful.

Do not wrap every operation in pointless nested Results.

Add:

-   centralized exception handling
-   problem/error mapping
-   stable error codes
-   exact field errors
-   supported values
-   correlation ID

Expected validation failures must not become 500 errors.

Unexpected exceptions must become safe, consistent server errors.

------------------------------------------------------------------------

# Phase 10 --- Logging and Observability

Implement:

-   structured logging
-   correlation/request IDs
-   request timing
-   exception logging
-   business-operation logging
-   metrics
-   distributed tracing
-   health checks

Use OpenTelemetry-compatible instrumentation.

Monitor:

-   request count
-   failure rate
-   latency
-   DB latency
-   borrow/return operations
-   bulk-import duration/failures

Never log secrets.

------------------------------------------------------------------------

# Phase 11 --- EF Core + Dapper

Use the right tool for the job.

### EF Core

Use for:

-   aggregate/entity persistence
-   writes
-   transactional domain operations
-   migrations
-   relationship management

### Dapper

Use selectively for:

-   complex read models
-   reports
-   optimized projections
-   high-performance read paths where justified

No technology should be introduced merely for résumé decoration.

------------------------------------------------------------------------

# Phase 12 --- Excel Bulk Import

Implement:

``` text
Upload
 ↓
File validation
 ↓
Header validation
 ↓
Parse
 ↓
Normalize
 ↓
Sanitize
 ↓
Validate every row
 ↓
Collect every safe error
 ↓
If any error → rollback
 ↓
Otherwise → transaction + bulk insert + commit
```

### Exact error requirements

Each row error must identify:

-   Excel line/row
-   field
-   error code
-   exact error message
-   required
-   supported values/constraints

One invalid tuple/row means:

> **ZERO rows from that import are committed.**

------------------------------------------------------------------------

# Phase 13 --- Security and Resilience

Add:

-   request size limits
-   upload size limits
-   file type validation
-   input sanitization
-   authorization strategy
-   secure configuration
-   secrets outside source control
-   rate limiting where appropriate
-   retry policy for transient failures
-   circuit breaker where justified
-   idempotency for operations that require it
-   concurrency protection

------------------------------------------------------------------------

# Phase 14 --- Documentation

Create and maintain:

``` text
docs/
├── MASTER_SPECIFICATION.md
├── ROADMAP.md
├── RELEASE-NOTES.md
├── ADR/
├── architecture/
├── database/
├── api/
├── testing/
└── templates/
```

Deliver:

-   ADRs
-   C4 context
-   C4 container
-   C4 component
-   DB/ER diagram
-   schema SQL
-   seed SQL
-   API/error contract
-   test strategy
-   Excel template
-   release notes

------------------------------------------------------------------------

# Phase 15 --- Release Notes API

Add:

``` http
GET /api/release-notes/current
```

Response must expose:

``` json
{
  "version": "x.y.z",
  "releaseDate": "YYYY-MM-DD",
  "newFeatures": [],
  "fixed": [],
  "qaChecklist": []
}
```

SQA should be able to use this endpoint as the authoritative "what
changed / what to test" entry point for the latest release.

------------------------------------------------------------------------

# Phase 16 --- Release Engineering

Before every release:

1.  Build backend.
2.  Build frontend.
3.  Run unit tests.
4.  Run integration tests.
5.  Run search regression.
6.  Run issue/return regression.
7.  Run bulk-import rollback tests.
8.  Verify database migrations.
9.  Verify seed data.
10. Verify API docs.
11. Verify release-notes endpoint.
12. Update `RELEASE-NOTES.md`.
13. Record release date/version.
14. Prepare QA checklist.

------------------------------------------------------------------------

# Definition of Enterprise Ready

The system is considered enterprise-ready only when it is:

-   functionally complete
-   persistently backed
-   transactionally safe
-   validation-safe
-   domain-invariant-safe
-   observable
-   testable
-   documented
-   secure
-   resilient
-   release-managed
-   QA-verifiable
-   maintainable

------------------------------------------------------------------------

# Guiding Rule

**Do not skip the MVP gate.**

The order is intentional:

``` text
Correct functionality
        ↓
Correct UX
        ↓
Correct validation/errors
        ↓
Correct tests
        ↓
MVP acceptance
        ↓
Architecture
        ↓
Persistence
        ↓
Observability/resilience
        ↓
Enterprise hardening
        ↓
Release-ready product
```

------------------------------------------------------------------------

# Phase 17 --- Smart Library Feature Set (added 2026-09-17)

Requested on top of the already-enterprise-ready MVP. Full detail, exact
file-level pointers and design caveats for each sub-phase are in
`docs/ai-handover.md` §4f (done) and §4g (ordered plan for the rest) - this
section is the durable summary; treat §4g as the source of truth for
implementation detail since it is kept current per checkpoint.

## 17.1 Auth (JWT, Librarian/Member RBAC) --- Done

JWT bearer auth, PBKDF2 password hashing, a `users` table/migration, login +
member self-registration + librarian-provisioning endpoints, every existing
controller now authorized (Books browsable by both roles, everything else
Librarian-only), a full login/register SPA flow with role-based nav and
routing. See `docs/ai-handover.md` §4f for the complete file list.

## 17.2 Book catalog enrichment --- Not started

Cover/thumbnail, optional edition, ebook/audiobook availability (design
decision: flags, not a single enum - a book can be physical AND ebook AND
audiobook simultaneously), external buy/PDF fallback links, and a new Book
detail page (none exists today) with "smart" availability resolution
(physical -> ebook -> audiobook -> external link).

## 17.3 Book-copy auto-generation --- Not started

`TotalCopies` on book creation generates that many sequential `BC-####`
`BookCopy` rows in one transaction, continuing the sequence rather than
restarting it.

## 17.4 Borrow limit 1 -> 2 --- Not started

Move from a boolean "has an active borrow" check to a configurable count
limit (`BorrowingOptions.MaxActiveBorrows`).

## 17.5 Member list filters / borrowing indicator --- Not started

Verify existing status search first (may already work); add a "currently
borrowing" column/filter if not already derivable.

## 17.6 Borrow-request / admin-approval workflow --- Not started

Net new: a member requests a borrow (or a purchase/new-title suggestion), a
librarian approval queue actions it via the existing `BorrowingService`.

## 17.7 Borrowing page search verification --- Not started

Re-verify member name/id, book name, copy id are all visible and searchable
before adding anything - most of this may already exist.

## 17.8 Localization audit --- Not started

Re-verify full-app Bangla coverage once 17.2/17.6 add new pages (they must
be built bilingual from the start via `t()`, not translated after the fact).

## 17.9 Voice search + agentic chat assistant --- Not started

Two provider tiers each, both configurable via appsettings/env vars:
- Speech: Web Speech API (client-side, default) + Hugging Face STT (server,
  configurable alternative).
- Chat: rule-based deterministic intent engine (default/fallback, no
  external dependency) + a real LLM path supporting both `ANTHROPIC_API_KEY`
  and `OPENAI_API_KEY` (user wants both available, selectable by config).
  The LLM path must call the same internal query "tools" rather than get
  free-form DB access. `guide.md` must document configuration for both,
  step by step.
