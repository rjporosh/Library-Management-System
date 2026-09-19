# ADR 0010 - Authenticated downloads go through the API client

**Status:** accepted (2026-09-19)

**Context.** Import templates are behind `[Authorize]`; a plain `<a href>` cannot send
the bearer token, so every download was a 401.

**Decision.** `downloadFile()` in `lib/api.ts` fetches a blob through the axios client
(auth + culture headers), honours `Content-Disposition`, and triggers a save. Callers
pass an API-relative path, so it also works when the SPA is served behind `/api`.

**Trade-offs.** The file is buffered in memory (templates are ~8 KB). Rejected:
short-lived signed URLs (extra endpoint and state for no benefit at this size).
