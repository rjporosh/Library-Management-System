# Configuration Reference

All keys live in `src/Library.Api/appsettings*.json`. Environment overrides use
`__` (e.g. `Database__Provider=InMemory`). Bound once in `Program.cs`.

## `Database`

| Key | Default | Meaning |
|---|---|---|
| `Provider` | `InMemory` | `InMemory` \| `Postgres` \| `SqlServer` \| `Sqlite` \| (`MySql`/`Oracle`/`Access`/`Mongo` throw) |
| `Orm` | `EfCore` | read-path ORM (`Dapper` reserved) |
| `ConnectionString` | `""` | required for a relational provider |
| `MigrateOnStartup` | `false` | apply migrations on start |
| `SeedOnStartup` | `true` | seed demo data when the DB is empty |

`appsettings.Development.json` → Postgres + `MigrateOnStartup: true`.
`appsettings.Production.json` → Postgres, no auto-migrate, `SeedOnStartup: false`.

## `BulkImport`

| Key | Default |
|---|---|
| `MaxFileSizeBytes` | `5242880` (5 MiB) |
| `MaxRows` | `5000` |
| `MaxErrors` | `500` |

## `FeatureFlags`

| Key | Default | Meaning |
|---|---|---|
| `LogsRootPath` | `logs` | folder (relative to content root or absolute) for the file log streams |
| `EnableRuntimeErrorLogging` | `true` | |
| `EnableBuildErrorLogging` | `true` | startup / DB-down diagnostics |
| `EnableQueryLogging` | `true` (Dev) / `false` (Prod) | per-command SQL log |
| `EnableExceptionLogging` | `true` | expected-exception log |
| `EnableMemberSuspensionCronJob` | `true` | nightly maintenance job + `/api/jobs/...run` |
| `EnableHealthCheckEndpoint` | `true` | `/health` |
| `EnableLogDownloadEndpoint` | `true` | `/api/logs/*` |
| `EnableOpenTelemetry` | `false` | OTLP traces + metrics |
| `OtlpEndpoint` | `http://localhost:4317` | OTLP gRPC collector |
| `ServiceName` | `Library.Api` | OpenTelemetry `service.name` |
| `EnableRateLimiting` | `true` | per-client fixed-window rate limiting |
| `RateLimitPermitPerWindow` | `120` | requests allowed per window per client |
| `RateLimitWindowSeconds` | `60` | window length |

## Localization

Culture: `?culture=` / `?lang=` query, then `Accept-Language`, then English.
Resources: `src/Library.Api/Resources/SharedResources[.<culture>].resx`.
See guide [13 — Localization](13-localization.md).

## Frontend (`frontend/library-web/.env`)

| Key | Meaning |
|---|---|
| `VITE_API_BASE_URL` | API base (`http://localhost:5254/api` dev, `/api` in the container) |
