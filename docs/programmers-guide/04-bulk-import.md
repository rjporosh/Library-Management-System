# Bulk Import

`GET /api/{resource}/import/template` → formatted `.xlsx`
`POST /api/{resource}/import` (multipart `file`) → `200 { success, imported }`
or `422` with every error.

## Pipeline (`BulkImportPipeline.RunAsync`)

1. File present / size ≤ `BulkImport:MaxFileSizeBytes`.
2. Read the first worksheet (`IWorkbookReader` → `ClosedXmlWorkbookReader`).
3. Header set must match the definition's `RequiredHeaders` — mismatch is
   rejected immediately (`IMPORT_HEADER_INVALID`).
4. Row-count ≤ `BulkImport:MaxRows`; at least one row.
5. Per row: normalise whitespace → **reject** any cell starting `= + @ TAB CR`
   (`IMPORT_CELL_FORMULA_REJECTED`) → `definition.ParseRowAsync` (field
   validation, stamped with the Excel row number).
6. In-file duplicate detection on the definition's `DuplicateKey`.
7. Database-conflict detection (`definition.FindExistingKeysAsync`).
8. **Preflight gate** — if there is *any* error, return them all; nothing is
   written.
9. Otherwise open a transaction, `PersistAsync`, `SaveChangesAsync`, commit.

Every `ApiError` carries `errorCode`, `errorMessage`, `field`, `line` (Excel
row), `required`, `supportedValues`. The list is capped at `BulkImport:MaxErrors`
with a `truncated` flag.

## Adding an entity

Implement `IImportDefinition<T>` in
`Features/BulkImport/Definitions/<T>ImportDefinition.cs`:

- `ResourceName`, `RequiredHeaders`, `Template` (an `ImportTemplateSpec`
  with columns + example rows for the downloadable `.xlsx`),
- `ParseRowAsync` — validate + build the entity, return `DuplicateKey`,
- `FindExistingKeysAsync` — which keys already exist in the DB,
- `PersistAsync` — `repository.AddRangeAsync(...)` (the pipeline saves + commits).

Register it in `ApplicationServiceExtensions.AddApplication`, add the three
methods to `BulkImportService`, and two controller actions.

Acceptance tests: `tests/Library.UnitTests/Features/BulkImport/` — the ten
`MASTER_SPECIFICATION.md §21` scenarios.
