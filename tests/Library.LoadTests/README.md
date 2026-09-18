# Library.LoadTests (NBomber)

Load / stress scenarios for the API. **Not run by `dotnet test`** — it's a
console app. Every scenario logs in as the seeded demo librarian
(`librarian` / `Librarian@123`) once at startup and reuses that bearer
token for every request, since all three endpoints require auth.

```bash
# 1. start the API - disable rate limiting so the load test measures real
#    application throughput instead of the per-client rate limiter (which
#    a single HttpClient sustaining 15-30 req/s will hit immediately - that
#    is the rate limiter working correctly, not something to "fix"):
FeatureFlags__EnableRateLimiting=false dotnet run --project src/Library.Api
#    (docker compose up -d works too, but its Production config has rate
#    limiting on by default - export the same env var for that container,
#    or point the load test at a local `dotnet run` instance instead)

# 2. run the load test
dotnet run --project tests/Library.LoadTests -c Release
dotnet run --project tests/Library.LoadTests -c Release -- http://localhost:5254
```

If you run this against a target with rate limiting **on** (the default),
expect most requests to come back `429 Too Many Requests` after the first
`FeatureFlags:RateLimitPermitPerWindow` (default 120) requests in a
60-second window - that is by design, not a load-test failure.

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
