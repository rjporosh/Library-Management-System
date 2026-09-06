# ADR 0007 — Observability & localization

**Status:** Accepted

## Decision — Observability
- **OpenTelemetry** traces + metrics over OTLP to Jaeger (appsettings-toggled;
  the exporter is resilient to a missing collector).
- **The custom file log streams stay** alongside OTel: `runtime-error-logs`,
  `build-error-logs` (incl. the diagnosed DB-down cause), `query-logs` (every
  SQL command — text, param names+types only, duration, rows, provider),
  `exception-logs`. They serve the "download a specific day's log file"
  requirement OTel does not, and are provider- and infrastructure-independent.
- `/health` does a real `CanConnectAsync` and names the failure cause.

## Decision — Localization
- **Resource-based**: `Resources/SharedResources.resx` (English, default) +
  `SharedResources.bn.resx` (Bangla). Adding a language is dropping in a
  `SharedResources.<culture>.resx` — no code change.
- Culture from the `Accept-Language` header or `?culture=` / `?lang=` query,
  fallback English.
- **The error contract is the localization surface.** `ApiError.errorCode` is
  stable; `GET /api/metadata/messages` returns `{errorCode: localizedText}` for
  the current culture, and the frontend maps codes to localized strings. This
  makes every validation / business / system message localizable without
  threading `IStringLocalizer` through every validator.
- The middleware localizes the generic 500 message and status titles directly.
