# ADR 0004 — Bulk-import transaction strategy

**Status:** Accepted

## Context
Excel import must be all-or-nothing: one bad / duplicate / malformed row and
nothing is written, with every detectable error returned together.

## Decision
- **A preflight gate, not compensation logic.** The pipeline validates the file,
  then every row (field rules, formula-injection rejection, in-file duplicates,
  database conflicts) and collects all errors. Only if there are **zero** errors
  does it open a transaction and insert the whole batch.
- Because nothing is inserted until validation fully passes, "roll back the
  whole file" is structural — it cannot half-fail. The transaction around the
  commit is belt-and-braces for a mid-insert DB fault.
- Header validation short-circuits (a malformed header is not a row error).
- Errors carry `errorCode`, `errorMessage`, `field`, `line` (Excel row),
  `required`, `supportedValues`; the list is capped with a `truncated` flag.
- ClosedXML (MIT) reads the workbook, behind an `IWorkbookReader` so the
  pipeline is unit-tested with a fake reader.

## Consequences
- Import works on the in-memory provider too (the gate is provider-agnostic);
  the real transaction only engages under a relational provider.
- All ten MASTER_SPECIFICATION §21 acceptance scenarios are covered by tests.
