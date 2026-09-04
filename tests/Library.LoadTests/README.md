# Library.LoadTests (NBomber)

Load / stress scenarios for the API. **Not run by `dotnet test`** — it's a
console app.

```bash
# 1. start the API
docker compose up -d            # or: dotnet run --project src/Library.Api

# 2. run the load test
dotnet run --project tests/Library.LoadTests -c Release
dotnet run --project tests/Library.LoadTests -c Release -- http://localhost:5254
```

Scenarios (30s each, injected rate):

| Scenario | Rate/s | Endpoint |
|---|---|---|
| `dashboard` | 30 | `GET /api/dashboard` |
| `books-search` | 20 | `POST /api/books/search` |
| `member-search` | 15 | `POST /api/members/search` |

Reports (txt + html) are written to `tests/Library.LoadTests/reports/`
(gitignored). Check p95/p99 latency and the error rate; add
`Scenario...WithThresholds(...)` to fail the run on a regression and wire it
into a nightly CI job.
