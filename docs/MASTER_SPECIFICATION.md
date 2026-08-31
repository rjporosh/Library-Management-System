# MASTER SPECIFICATION --- Library Management System

**Document:** `MASTER_SPECIFICATION.md`\
**Status:** Living specification\
**Baseline:** MVP → Enterprise-grade production evolution\
**Primary objective:** Build a real-life usable, sellable Library
Management System without sacrificing the original OOP/OOAD learning
objectives.

------------------------------------------------------------------------

## 1. Product Vision

The Library Management System (LMS) shall manage books, physical book
copies, members, and borrowing/return operations through a responsive
web UI and a clean HTTP API.

The project is deliberately delivered in two stages:

1.  **Functional MVP first:** all required user journeys, validation,
    search, UI feedback, tests, and documentation must work correctly.
2.  **Enterprise hardening second:** after the MVP gate is passed,
    evolve the implementation toward production-grade architecture,
    persistence, observability, resilience, security, and operational
    readiness.

No enterprise abstraction is considered complete merely because the
type/interface exists. Every feature must be behaviorally implemented
and testable.

------------------------------------------------------------------------

## 2. Scope and Non-Goals

### 2.1 MVP scope

-   Book management
-   Physical book-copy management
-   Member management
-   Borrow/issue workflow
-   Return workflow
-   Book/member details
-   Availability tracking
-   Dashboard
-   Search, filtering, sorting, and pagination
-   Responsive frontend
-   SweetAlert2 confirmations/success/error notifications
-   Validation and graceful errors
-   Unit and integration tests
-   OOP/OOAD deliverables

### 2.2 Enterprise evolution scope

-   Vertical Slice Architecture
-   CQRS
-   MediatR
-   Domain invariants and state protection
-   Result pattern
-   Centralized exception handling
-   Standard error contract
-   Structured logging
-   Correlation/request identifiers
-   Observability
-   Persistence/database abstraction
-   EF Core and Dapper
-   Transactions and concurrency
-   Excel bulk import
-   Sanitization and import validation
-   ADRs
-   C4 architecture diagrams
-   DB/ER diagrams
-   SQL schema and seed data
-   Release notes and release-notes API
-   QA/release verification workflow
-   Production-grade security/resilience

------------------------------------------------------------------------

## 3. Functional Domain

### 3.1 Book

A book represents the bibliographic work.

Required fields:

-   `bookId`
-   `title`
-   `author`
-   `isbn`
-   `category`
-   `publisher`
-   `description` (optional)
-   `publishedYear`

Business expectations:

-   Title is required.
-   Author is required.
-   ISBN is required and must satisfy the configured ISBN validation
    rule.
-   Category and publisher must be represented in the production domain
    model.
-   Published year must be valid.
-   Duplicate ISBN handling must be deterministic.

### 3.2 Book Copy

A book copy represents a physical copy of a book.

Required concepts:

-   `bookCopyId`
-   `bookId`
-   copy identifier/barcode
-   status

Supported statuses shall be explicit and documented, for example:

-   `Available`
-   `Borrowed`
-   `Lost`
-   `Damaged`
-   `Maintenance`

The exact set used by the implementation must be centralized rather than
scattered as magic strings.

### 3.3 Member

Required concepts:

-   `memberId`
-   membership number
-   name
-   phone
-   address
-   status

Email may be retained where useful, but phone/address required by the
product specification must not be silently omitted.

Member status must be explicit, such as:

-   `Active`
-   `Inactive`
-   `Suspended`

The final accepted values must be exposed by validation/error metadata.

### 3.4 Borrow Record

A borrow record links:

-   member
-   physical book copy
-   issue date
-   due date
-   return date
-   status

The system must prevent invalid state transitions.

Examples:

-   An unavailable copy cannot be issued.
-   A returned borrow cannot be returned twice.
-   An inactive/suspended member cannot be issued a book.
-   A copy cannot have two active borrow records.

------------------------------------------------------------------------

## 4. Required User Journeys

### 4.1 Book management

The librarian shall be able to:

-   list books
-   search books
-   sort books
-   paginate books
-   view book details
-   create a book
-   update a book
-   validate input
-   receive clear success/error feedback

### 4.2 Book-copy management

The librarian shall be able to:

-   list copies
-   associate copies with books
-   see availability
-   add/update copies
-   prevent invalid duplicate copy identifiers
-   see current borrowing information when applicable

### 4.3 Member management

The librarian shall be able to:

-   list members
-   search members
-   view member details
-   create/update members
-   see active/current borrowings
-   see borrowing history
-   see member borrowing summary

Member detail should expose, where applicable:

-   total borrowed
-   currently borrowed
-   overdue
-   last borrowed
-   current books
-   borrowing history

### 4.4 Issue/borrow

The intended UX is librarian-friendly, not UUID-centric.

Flow:

1.  Search member by membership ID, phone, or supported member
    identifier.
2.  Display matching member information.
3.  Select an available physical copy.
4.  Confirm due date.
5.  Confirm issue.
6.  Create borrow record.
7.  Change copy status to borrowed.
8.  Show SweetAlert2 success confirmation.

### 4.5 Return

The librarian shall be able to locate the active borrowing using
supported copy/member/book identifiers rather than being forced to know
an internal borrow-record UUID.

Flow:

1.  Search active borrowing.
2.  Display book, copy, member, issue date, and due date.
3.  Confirm return.
4.  Set return date.
5.  Mark borrow record returned.
6.  Mark copy available.
7.  Show SweetAlert2 success confirmation.

------------------------------------------------------------------------

## 5. Search Specification

Search is a first-class feature and must be consistent across API and
UI.

### 5.1 GitLab-style multi-select search

The frontend shall provide a multi-select field selector, for example:

``` text
Search in:
☑ Title
☑ Author
☐ ISBN
```

The user may select one or multiple fields.

When multiple fields are selected, the search term matches **ANY
selected field (OR semantics)**.

Example:

``` text
Selected: Title + Author
Query: clean
```

means:

``` text
Title contains "clean"
OR
Author contains "clean"
```

### 5.2 Search rules

-   Case-insensitive
-   Trim surrounding whitespace
-   Contains semantics unless a field explicitly defines another
    operator
-   Empty query means no text filter
-   If no search field is selected, default to **Title**
-   Multiple selected values/terms must be supported where the UI
    exposes multi-value search
-   Search must work together with pagination and sorting
-   API and frontend behavior must be identical
-   Unsupported fields must never be displayed as selectable UI options

### 5.3 Supported book search fields

At MVP:

-   Title
-   Author
-   ISBN

Future enterprise filters may include:

-   Category
-   Publisher
-   Published year
-   Copy status

### 5.4 Pagination

Pagination metadata must be exposed through a documented API contract,
including:

-   current page
-   page size
-   total items
-   next page indicator/link where applicable

------------------------------------------------------------------------

## 6. Validation and Error Contract

Validation is part of the product contract, not merely a controller
implementation detail.

Every applicable error shall use the common structure:

``` json
{
  "success": false,
  "errors": [
    {
      "errorCode": "BOOK_ISBN_REQUIRED",
      "errorMessage": "ISBN is required.",
      "field": "isbn",
      "line": 14,
      "required": true,
      "supportedValues": "ONLY NUMBER, MIN DIGIT 8, MAX DIGIT 16"
    }
  ]
}
```

### 6.1 Error properties

-   `errorCode`: stable machine-readable code
-   `errorMessage`: exact human-readable error
-   `field`: exact API/domain/import field
-   `line`: source line/Excel row when applicable; omitted/null for
    non-row requests
-   `required`: whether the field is required
-   `supportedValues`: exact accepted values or exact validation
    constraints

### 6.2 Supported-values examples

For a constrained string:

``` json
"supportedValues": ["Active", "Inactive", "Suspended"]
```

For a format/range:

``` json
"supportedValues": "ONLY NUMBER, 4 DIGITS, BETWEEN 1000 AND CURRENT YEAR"
```

For ISBN:

``` json
"supportedValues": "ONLY NUMBER, MIN DIGIT 8, MAX DIGIT 16"
```

The final implementation must keep these rules synchronized with actual
validation logic.

### 6.3 Multiple errors

All detectable validation errors should be returned together where safe
and practical:

``` json
"errors": [
  {},
  {},
  {}
]
```

The frontend must be able to map errors to the exact field and display
the exact message.

------------------------------------------------------------------------

## 7. Static Error Resources

Error codes and validation metadata shall eventually be backed by
centralized static resources/configuration rather than duplicated string
literals throughout the codebase.

Requirements:

-   stable codes
-   human-readable messages
-   supported values/rules
-   field mapping
-   localization-ready structure
-   API documentation
-   no secret/business-sensitive data in client-visible resources

------------------------------------------------------------------------

## 8. Excel Bulk Insertion

Bulk book insertion shall accept a documented, formatted Excel template.

### 8.1 Import pipeline

``` text
Excel upload
  ↓
File/type/size validation
  ↓
Header/schema validation
  ↓
Row parsing
  ↓
Normalization
  ↓
Sanitization
  ↓
Field validation
  ↓
Duplicate/business-rule validation
  ↓
Pre-flight validation of ALL rows
  ↓
Database transaction
  ↓
Bulk insert
  ↓
Commit
```

### 8.2 All-or-nothing rule

If any row is missing, malformed, duplicated, or invalid:

-   no partial insert may remain
-   the transaction must be rolled back
-   all safely detectable errors should be returned
-   every error must identify the exact Excel line/row
-   every error must identify the exact field
-   every error must provide the exact reason
-   `supportedValues` must explain the expected value/rule

Example:

``` json
{
  "success": false,
  "errors": [
    {
      "errorCode": "BOOK_ISBN_REQUIRED",
      "errorMessage": "ISBN is required.",
      "field": "isbn",
      "line": 14,
      "required": true,
      "supportedValues": "ONLY NUMBER, MIN DIGIT 8, MAX DIGIT 16"
    },
    {
      "errorCode": "BOOK_YEAR_INVALID",
      "errorMessage": "Published year must be a valid year.",
      "field": "publishedYear",
      "line": 27,
      "required": true,
      "supportedValues": "ONLY NUMBER, 4 DIGITS, BETWEEN 1000 AND CURRENT YEAR"
    }
  ]
}
```

### 8.3 Import safety

-   Do not execute partially validated rows.
-   Do not trust Excel cell content.
-   Normalize whitespace and expected formats.
-   Reject malformed headers.
-   Detect duplicate rows within the upload.
-   Detect conflicts with existing database records.
-   Protect against formula/content injection where relevant.
-   Enforce maximum file size and row count.
-   Log import correlation information without leaking sensitive data.

------------------------------------------------------------------------

## 9. SweetAlert2 UX Standard

SweetAlert2 shall be genuinely implemented, not merely installed as a
dependency.

Use it for:

-   successful create/update
-   successful issue/return
-   destructive confirmations
-   bulk-import success/failure
-   important validation failures
-   unexpected operation failures

Rules:

-   no fake success
-   no success popup before API success
-   confirmation must precede destructive actions
-   exact backend error messages should be displayed
-   loading state should prevent duplicate submissions
-   alerts should not hide actionable field-level validation

------------------------------------------------------------------------

## 10. Dashboard

Dashboard shall provide live system information such as:

-   total books
-   total physical copies
-   available copies
-   borrowed copies
-   total members
-   recent borrowing activity

Dashboard values must come from authoritative backend data.

The enterprise version must avoid N+1 API patterns for aggregate
dashboard data.

------------------------------------------------------------------------

## 11. API Standards

All endpoints shall have:

-   predictable resource naming
-   correct HTTP verbs/status codes
-   validation
-   consistent response shapes
-   pagination where applicable
-   documented error contract
-   correlation/request ID in enterprise phase
-   OpenAPI documentation
-   release compatibility considerations

------------------------------------------------------------------------

## 12. Enterprise Architecture

After MVP acceptance, evolve toward:

``` text
API
 │
 ├── Vertical Slice Features
 │      ├── Books
 │      ├── Book Copies
 │      ├── Members
 │      ├── Borrowing
 │      ├── Bulk Import
 │      └── Release Notes
 │
 ├── Application / Use-case orchestration
 ├── Domain
 │      ├── Entities
 │      ├── Value Objects
 │      ├── Invariants
 │      └── Domain rules
 └── Infrastructure
        ├── EF Core
        ├── Dapper
        ├── DB providers
        ├── Transactions
        └── External infrastructure
```

### Required enterprise capabilities

-   VSA
-   CQRS
-   MediatR
-   centralized exception middleware/filter
-   Result pattern where appropriate
-   domain invariants
-   validation pipeline
-   structured logging
-   correlation ID
-   observability
-   resilience/retry/circuit-breaker where justified
-   database abstraction/provider strategy
-   EF Core for rich persistence/domain operations
-   Dapper for appropriate read/reporting workloads
-   transaction boundaries
-   concurrency protection
-   security hardening
-   configuration management
-   health checks

------------------------------------------------------------------------

## 13. Invalid-State Protection

The domain must not permit impossible states.

Examples:

-   Borrowed copy must have one active borrow.
-   Available copy must not have an active borrow.
-   Returned borrow cannot be returned again.
-   Suspended member cannot borrow.
-   Due date cannot violate configured business rules.
-   Duplicate ISBN/copy/member identifiers must be rejected according to
    business rules.

Validation should happen at the appropriate layers, but critical domain
invariants must not depend solely on controller checks.

------------------------------------------------------------------------

## 14. Persistence Strategy

MVP may use in-memory repositories where explicitly accepted.

Enterprise phase shall introduce:

-   relational database
-   migrations/schema management
-   EF Core
-   Dapper for optimized read paths where justified
-   repository/query strategy only where it provides real value
-   transaction management
-   optimistic concurrency where needed
-   indexes for search and foreign keys
-   database constraints for critical uniqueness/integrity rules

Supported database abstraction must be designed so the application does
not become unnecessarily coupled to one provider.

------------------------------------------------------------------------

## 15. Logging and Observability

Enterprise phase shall include:

### Logging

-   structured logs
-   log levels
-   correlation/request IDs
-   operation duration where useful
-   exception details
-   no passwords/secrets/sensitive personal data in logs

### Observability

-   traces
-   metrics
-   health checks
-   request/error rates
-   database performance visibility
-   import duration/failure metrics

A practical target stack may include OpenTelemetry plus suitable
exporters/backends.

------------------------------------------------------------------------

## 16. Documentation Deliverables

The repository must contain:

``` text
docs/
├── MASTER_SPECIFICATION.md
├── ROADMAP.md
├── RELEASE-NOTES.md
├── ADR/
├── architecture/
│   ├── C4-Context
│   ├── C4-Container
│   └── C4-Component
├── database/
│   ├── schema.sql
│   ├── seed-data.sql
│   └── diagrams
├── api/
├── testing/
└── templates/
    └── bulk-book-import.xlsx
```

The original requirements document must remain attached/referenceable.

------------------------------------------------------------------------

## 17. Required Architecture Deliverables

### ADRs

ADRs must explain important decisions, including:

-   architecture
-   CQRS
-   MediatR
-   persistence
-   EF Core vs Dapper
-   error contract
-   bulk import transaction strategy
-   observability
-   search design
-   resilience/security decisions where relevant

### C4

Provide:

1.  Context diagram
2.  Container diagram
3.  Component diagrams for important slices

### Database diagrams

Provide:

-   ER/database schema diagram
-   entity relationships
-   keys
-   indexes
-   important constraints

------------------------------------------------------------------------

## 18. SQL Deliverables

Provide:

-   schema SQL
-   constraints
-   indexes
-   seed SQL
-   deterministic demo data
-   migration strategy/documentation

Seed data must support QA demonstrations for:

-   available copy
-   borrowed copy
-   overdue case
-   active member
-   inactive/suspended member
-   searchable books
-   multiple authors/categories where supported

------------------------------------------------------------------------

## 19. Release Notes

Maintain `RELEASE-NOTES.md`.

Every release entry must contain:

-   version
-   release date
-   new features
-   fixed defects
-   changed behavior
-   known issues
-   QA checklist
-   regression areas
-   migration/database notes when applicable

Expose the current release through an API endpoint such as:

``` http
GET /api/release-notes/current
```

The endpoint shall expose at least:

-   version
-   release date
-   new features
-   fixes
-   QA checks

------------------------------------------------------------------------

## 20. OOP / OOAD Deliverables

The original educational objective remains mandatory.

Document:

-   identified classes
-   responsibilities
-   relationships
-   associations
-   aggregation/composition where applicable
-   inheritance only where justified
-   interfaces/abstractions
-   use cases
-   UI screens
-   core business logic
-   unit/integration testing approach

The architecture must demonstrate good OO principles without introducing
abstraction for abstraction's sake.

------------------------------------------------------------------------

## 21. Testing Strategy

Required layers:

### Unit tests

-   domain rules
-   validation
-   search
-   sorting
-   borrowing rules
-   return rules
-   bulk row validation
-   error mapping

### Integration tests

-   API endpoints
-   persistence
-   transaction rollback
-   search/pagination
-   issue/return workflows
-   bulk import

### Frontend verification

-   production build
-   API error rendering
-   search UI
-   multi-select behavior
-   SweetAlert flows
-   responsive screens

### Bulk-import acceptance tests

At minimum:

1.  all valid rows → commit
2.  one missing required field → rollback all
3.  one invalid ISBN → rollback all
4.  one invalid year → rollback all
5.  duplicate ISBN within file → rollback all
6.  duplicate ISBN against database → rollback all
7.  malformed header → reject
8.  exact row/field error returned
9.  multiple errors returned together
10. exact supported values/rules returned

------------------------------------------------------------------------

## 22. Definition of Done

A feature is not Done unless:

-   backend implemented
-   frontend implemented where applicable
-   validation implemented
-   errors use the common contract
-   success/failure UX implemented
-   SweetAlert2 behavior implemented where applicable
-   tests added
-   existing tests remain green
-   API documentation updated
-   release notes updated
-   no dead/unused code
-   no unsupported UI options
-   no silent partial data writes
-   no known contract mismatch between frontend and backend

------------------------------------------------------------------------

## 23. MVP Gate

The MVP is accepted only when:

-   frontend builds successfully
-   all core screens work
-   all required workflows work end-to-end
-   search is correct and consistent
-   multi-select search works
-   issue/return state transitions are correct
-   member/book details are complete
-   SweetAlert2 is correctly implemented
-   validation is graceful and actionable
-   tests pass
-   original requirements and deliverables are satisfied

Only after this gate shall enterprise architecture work begin.

------------------------------------------------------------------------

## 24. Product Quality Principle

The target is not "a demo that looks good."

The target is:

> **A maintainable, testable, observable, secure, database-backed
> library platform that can realistically be deployed and sold.**

Correctness first. Architecture second. Production hardening third. No
cargo-cult enterprise patterns.
