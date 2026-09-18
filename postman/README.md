# Postman collection

`Library-Management-System.postman_collection.json` — every REST endpoint (64),
grouped into folders, each request with a description and a ready-to-send example
body. `Library-Management-System.postman_environment.json` — the matching
environment (`baseUrl`, seeded demo credentials).

Every request except `Auth`, `Metadata`, `Release Notes` and `Health` requires a
bearer token (auth was added after this collection was first built). The
collection's **default auth is `Bearer {{bearerToken}}`**, so you only need to
log in once - every other request picks the token up automatically.

## Run it

```bash
# 1. start the API (in-memory store, no infra needed)
dotnet run --project src/Library.Api          # http://localhost:5254

# 2a. Postman: import both files, pick "Library MS - Local", open
#     Auth > "1. Login as librarian" and hit Send once (sets `bearerToken`
#     for the rest of the collection), then send any other request as-is.
# 2b. CLI - the whole collection, in one shot, no login step needed first
#     (Auth > Login runs automatically as request #1):
npx newman run postman/Library-Management-System.postman_collection.json \
    -e postman/Library-Management-System.postman_environment.json

# 2c. CLI - just the self-contained smoke flow:
npx newman run postman/Library-Management-System.postman_collection.json \
    -e postman/Library-Management-System.postman_environment.json \
    --folder "Smoke Flow (run in order)"
```

Seeded demo accounts (see `docs/ai-handover.md`): `librarian` / `Librarian@123`
(Librarian) and `alice@example.com` / `Member@123` (Member). Both are already
filled in as environment variables.

## Folders

**Auth** · Books · Book Copies · Members · Borrowing · **Borrow Requests** ·
Dashboard · Jobs · Metadata · Release Notes · Logs · Health ·
**Smoke Flow (run in order)**

- **Auth**: login as librarian/member, self-service member registration,
  librarian-provisioning. Run **"1. Login as librarian"** once and every other
  folder's requests authenticate automatically.
- **Borrow Requests**: the member borrow/purchase-request workflow and the
  librarian approval queue. The two Member-only requests use
  `{{memberBearerToken}}` explicitly (run Auth > "2. Login as member" first);
  everything else uses the collection default.
- **Smoke Flow** is a fully self-contained, runnable end-to-end happy path for
  the Collection Runner: log in → create book → copy → member → issue →
  (blocked delete) → return → cascade-delete → confirm 404. IDs and the token
  are chained through collection variables and randomized where re-running
  matters (ISBN, barcode, membership number), so it needs no manual editing
  and can be run any number of times.

Verified with `newman` against a freshly-started API: the whole collection
(64 requests, 17/17 assertions) and the Smoke Flow folder in isolation
(10 requests, 12/12 assertions) both pass cleanly. If you run the whole
collection back-to-back many times against the same process in a short
window, you may see `429 Too Many Requests` near the end - that's the
API's per-client rate limiter (120 requests/60s by default) correctly
doing its job, not a collection bug; start a fresh API process (or set
`FeatureFlags__EnableRateLimiting=false`) if you need to iterate quickly.

Regenerate after endpoint changes: `python3 postman/build_collection.py`.
