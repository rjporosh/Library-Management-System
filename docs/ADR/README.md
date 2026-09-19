# Architecture Decision Records

Short records of the decisions that shaped the enterprise build. Each states the
context, the decision, and the trade-off accepted.

| ADR | Decision |
|---|---|
| [0001](0001-persistence-and-provider-strategy.md) | EF Core + PostgreSQL primary, configuration-only provider switching |
| [0002](0002-efcore-vs-dapper.md) | EF Core for writes + search; Dapper for the dashboard aggregate |
| [0003](0003-advanced-search-expression-builder.md) | Hand-built expression trees, not dynamic-LINQ |
| [0004](0004-bulk-import-transaction-strategy.md) | Preflight-gate all-or-nothing import |
| [0005](0005-result-pattern-and-envelope.md) | Result pattern on new features; RFC 7807 envelope; success envelope deferred |
| [0006](0006-soft-delete-and-cascade.md) | Soft delete everywhere + smart cascade-delete confirmation |
| [0007](0007-observability-and-localization.md) | OpenTelemetry + file log streams; resource-based localization |
| [0008](0008-jaeger-basic-auth-proxy.md) | Jaeger UI published only through an nginx basic-auth proxy |
| [0009](0009-voice-chat-client-side-speech.md) | Browser STT/TTS + deterministic question parser for voice chat |
| [0010](0010-authenticated-file-download.md) | Authenticated blob download instead of plain links |
