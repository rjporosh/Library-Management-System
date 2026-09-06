# ADR 0005 — Result pattern & response envelope

**Status:** Accepted

## Context
The spec wants a unified Result pattern, all validation errors returned
together, and an RFC 7807-style contract.

## Decision
- **New features return `Result` / `Result<T>`** carrying either success or a
  non-empty `ApiError` list. `ResultActionExtensions` maps the leading error
  code to an HTTP status (`_NOT_FOUND`→404, `_DUPLICATE`/conflict→409,
  row-level→422, feature-disabled→403, else 400).
- **Existing exception-based endpoints were migrated opportunistically**, not in
  a big-bang rewrite — the `GlobalExceptionHandlingMiddleware` already produces
  the correct envelope for thrown exceptions.
- **Failure responses are RFC 7807** `application/problem+json`
  (`type/title/status/detail/instance`) with `success` / `errors[]` /
  `correlationId` as extension members. `AddProblemDetails()` gives framework
  errors the same extension members.
- **A success envelope (`{success,message,data,traceId}`) is deferred.** It
  would break the frontend and every integration test at once. Successful
  responses return the raw DTO; the error contract is the consistent surface.

## Consequences
- Consistent error handling without a disruptive rewrite.
- The success envelope can be introduced later as an opt-in filter or at a major
  version with a coordinated frontend change.
