# Troubleshooting

| Symptom | Cause / fix |
|---|---|
| API exits on startup, `logs/build-error-logs/` says "Cannot reach the Postgres database server" | Postgres isn't running. `docker compose up -d db`, or run with `Database__Provider=InMemory`. |
| `logs/build-error-logs/` says "database does not exist" | `dotnet ef database update` (see `MIGRATIONS.md`). |
| `logs/build-error-logs/` says "authentication failed" | wrong username/password in `Database:ConnectionString`. |
| `/health` returns `Unhealthy` | the description names the reason — usually the DB is down or the schema is behind. |
| `dotnet build` fails with a warning | `TreatWarningsAsErrors` is on by design. Fix the warning; don't suppress it. |
| Integration tests fail wanting a database | they should use `LibraryApiFactory` (pins `Database:Provider=InMemory`). A test using `new WebApplicationFactory<Program>()` will pick up the Development Postgres config. |
| `dotnet ef` says "pending model changes" | you changed an entity/`OnModelCreating` without a migration. `dotnet ef migrations add <Name> …`. |
| Advanced search returns `SEARCH_FIELD_UNKNOWN` | the field isn't in that entity's `SearchMap`. Add a `.Field(...)` line. |
| Bulk import always rejects with `IMPORT_HEADER_INVALID` | the header row must contain the definition's `RequiredHeaders` (case-insensitive). Start from the downloaded template. |
| Frontend shows the generic support-message alert | the API returned a 5xx — check `logs/runtime-error-logs/` with the `traceId` shown. |
| Frontend can't reach the API | check `VITE_API_BASE_URL` and CORS (`Program.cs` allows `http://localhost:5173`). |
| Jaeger shows no traces | `FeatureFlags:EnableOpenTelemetry` must be `true` and `OtlpEndpoint` must point at the collector. |
