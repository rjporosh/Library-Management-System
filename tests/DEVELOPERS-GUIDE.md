# Tests — Developer Guide

## Projects

| Project | Kind | Notes |
|---|---|---|
| `Library.UnitTests` | domain + application | xUnit. References `Library.Infrastructure` so tests can use the real `InMemory*Repository` + `NoOpUnitOfWork` instead of hand fakes. |
| `Library.IntegrationTests` | in-process HTTP | `Microsoft.AspNetCore.Mvc.Testing`. Uses `LibraryApiFactory` which pins `Database:Provider=InMemory` — **no database needed**. |

## Run

```bash
dotnet test LibraryManagementSystem.slnx                 # everything (60)
dotnet test tests/Library.UnitTests                      # just unit
dotnet test --filter "FullyQualifiedName~BulkImport"     # by name
```

Frontend gate:

```bash
cd frontend/library-web && npm run lint && npm run build
```

## Writing a test

- **Service unit test** — construct the real `InMemory<X>Repository` from
  `Library.Infrastructure.Persistence.Repositories.InMemory`, seed via its
  `Seed(...)` method, pass `new NoOpUnitOfWork()`, assert on `Result` /
  entity state.
- **Integration test** — `IClassFixture<LibraryApiFactory>`, call `factory
  .CreateClient()`, deserialize with `TestJson.Options` / `.ReadModelAsync<T>()`
  (enum-aware).
- **Bulk-import acceptance** — `tests/Library.UnitTests/Features/BulkImport/`
  covers the ten `MASTER_SPECIFICATION.md §21` scenarios with a fake
  `IWorkbookReader`.

## Coverage

`coverlet.collector` is referenced; add `--collect:"XPlat Code Coverage"` to
`dotnet test` and point ReportGenerator at the produced `coverage.cobertura.xml`.

## Load / stress

`tests/Library.LoadTests` (NBomber) is scaffolded — `dotnet run --project
tests/Library.LoadTests -c Release` against a running API. See its README.
