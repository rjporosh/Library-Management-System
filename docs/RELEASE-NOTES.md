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

# Release 0.1.0 --- MVP Baseline

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
