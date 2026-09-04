# Observability

## OpenTelemetry (traces + metrics)

Enable with `FeatureFlags:EnableOpenTelemetry = true` and set
`FeatureFlags:OtlpEndpoint` (default `http://localhost:4317`; compose uses
`http://jaeger:4317`).

- Instrumented: ASP.NET Core, HttpClient, Npgsql, .NET runtime, plus the
  `"Library"` `ActivitySource` and `Meter` (wire custom spans/counters from
  Application code).
- View traces in Jaeger: http://localhost:16686 → service `Library.Api`.
- Wiring is in `src/Library.Api/Observability/OpenTelemetryExtensions.cs`.

## Structured file logs (`IAppLogWriter`)

One JSON-lines file per category per day under the API content root's `logs/`:

| Folder | Contents |
|---|---|
| `runtime-error-logs/` | unhandled 500s and background-job faults (root cause, stack, fix hint, correlation id) |
| `build-error-logs/`   | startup failures — including the **diagnosed DB-down cause** (provider, host, database, fix) |
| `query-logs/`         | every EF/Dapper command: SQL text, parameter **names + types only**, duration, rows, provider |
| `exception-logs/`     | expected business exceptions mapped to 4xx |

Download a day's file: `GET /api/logs/available` then
`GET /api/logs/download?category=…&date=dd-MM-yyyy` (gated by
`FeatureFlags:EnableLogDownloadEndpoint`).

Query logging is gated by `FeatureFlags:EnableQueryLogging` (on in Development,
off in Production by default).

## Health

`GET /health` (gated by `FeatureFlags:EnableHealthCheckEndpoint`) returns a JSON
body a load balancer / uptime monitor can read; for a relational provider it
does a real `CanConnectAsync` and, on failure, the description names the
provider and the reason.

## Correlation id

`CorrelationIdMiddleware` reads or creates `X-Correlation-Id`, echoes it on the
response, and every log entry is tagged with it.
