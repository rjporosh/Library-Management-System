# ROADMAP --- Library Management System

**Strategy:** Complete the MVP first, verify it rigorously, then evolve
it into an enterprise-grade product.

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
