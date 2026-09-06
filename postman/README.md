# Postman collection

`Library-Management-System.postman_collection.json` — every REST endpoint (43),
grouped into folders, each request with a description and a ready-to-send example
body. `Library-Management-System.postman_environment.json` — the matching
environment (`baseUrl = http://localhost:5254`).

## Run it

```bash
# 1. start the API (in-memory store, no infra needed)
dotnet run --project src/Library.Api          # http://localhost:5254

# 2a. Postman: Import both files, pick "Library MS - Local", send any request.
# 2b. CLI:
npx newman run postman/Library-Management-System.postman_collection.json \
    --folder "Smoke Flow (run in order)"
```

## Folders

Books · Book Copies · Members · Borrowing · Dashboard · Jobs · Metadata ·
Release Notes · Logs · Health · **Smoke Flow (run in order)**

The **Smoke Flow** folder is a runnable end-to-end happy path for the Collection
Runner: create book → copy → member → issue → (blocked delete) → return →
cascade-delete → confirm 404. IDs are chained through collection variables, so it
needs no manual editing. Verified with `newman` (10/10 assertions).

Regenerate after endpoint changes: `python3 postman/build_collection.py`.
