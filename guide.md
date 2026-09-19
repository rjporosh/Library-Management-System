# Library Management System — Start-to-Finish Guide

A step-by-step walkthrough of running, using, testing and observing the system.

---

## 1. What's in the box

| Part | Path | Stack |
|---|---|---|
| Backend API | `src/Library.{Domain,Application,Infrastructure,Api}` | .NET 10, EF Core, in-memory/PostgreSQL |
| Frontend | `frontend/library-web` | React 19, Vite, Tailwind, TanStack Query |
| Tests | `tests/Library.{UnitTests,IntegrationTests}` | xUnit |
| Docs | `docs/` + `MIGRATIONS.md` + this file | Markdown |

---

## 2. Run everything with Docker (recommended)

```bash
docker compose up --build
```

One command, four containers — PostgreSQL, Jaeger, the API and the web UI
(`docker-compose.yml`). It is a multi-container app, not one image: the SPA is
built and served by nginx, which proxies `/api` to the API container.

| Service | URL |
|---|---|
| Web UI | http://localhost:8080 |
| API + OpenAPI (Scalar) | http://localhost:5254 / http://localhost:5254/scalar |
| Jaeger (traces) | http://localhost:16686 — login `jaeger` / `Jaeger@123` (basic-auth proxy, see ADR 0008) |
| PostgreSQL | localhost:5433 (`library` / `library`) |

The API migrates and seeds the database on first start. Stop with
`docker compose down`; add `-v` to also wipe the database volume (do this if a
volume left over from an older schema stops the API booting).

---

## 3. Run locally (no Docker)

### Backend

```bash
# Option A — with PostgreSQL:
docker compose up -d db
dotnet run --project src/Library.Api            # http://localhost:5254

# Option B — zero dependencies (in-memory):
Database__Provider=InMemory dotnet run --project src/Library.Api
```

### Frontend

```bash
cd frontend/library-web
npm install
npm run dev                                     # http://localhost:5173
```

`frontend/library-web/.env` points the SPA at `http://localhost:5254/api`.

---

## 4. Using the app

**Sign in first** — every page except Login/Register requires an account.
Seeded demo logins (see §4a for how to add your own):

| Role | Username / Email | Password |
|---|---|---|
| Librarian | `librarian` | `Librarian@123` |
| Member | `alice@example.com` | `Member@123` |

1. **Dashboard** (Librarian only) — live totals (books, copies by status,
   members by status, overdue borrows) plus **Run membership maintenance** to
   suspend overdue borrowers and mark expired memberships inactive immediately.
2. **Books** — every signed-in user can browse; only a Librarian sees
   Add/Edit/Delete/Bulk-import. Click a book for its **detail page**: cover
   (or an auto-derived Open Library cover by ISBN), edition, and "smart
   availability" — a physical copy if one is free, else an ebook/audiobook
   link, else a suggested external buy/PDF link. Creating a book with
   **Total copies** > 0 auto-generates that many sequentially-barcoded copies
   (`BC-0001`, `BC-0002`, ...).
3. **Book Copies / Members** (Librarian only) — each page has:
   - a **quick search** box and a **Filters** panel (GitLab-style: add
     `field / operator / value` rows, AND or OR, multi-field sort);
   - **status badges** (colour-coded);
   - **Bulk import** — download the `.xlsx` template, fill it, upload. If any
     row is invalid or duplicated the whole file is rejected and every error is
     shown with its exact row, field and accepted values.
4. **Members** — the row menu has Suspend / Reactivate / Renew / Mark inactive /
   Delete; click a name for the member detail (borrowing summary + history,
   showing the book title and copy barcode, not raw ids).
5. **Borrowing** (Librarian only) — search a member and an available copy by
   name/barcode (no UUIDs, and searchable by **voice** via the mic button - see
   §4b), pick a due date, confirm. A member may borrow at most
   `Borrowing:MaxActiveBorrowsPerMember` books at once (default 2). Return
   from the active-borrows table, which shows the real member name and book
   title and is itself searchable.
6. **Requests** — a Member can request to borrow a title or suggest a
   purchase from the Book detail page / this page; a Librarian sees an
   approval queue here (Approve issues the book immediately if a copy is free).
7. **Library Assistant** (Librarian only) — the floating chat button, bottom
   right. Ask things like *"How many copies of Clean Code are available?"*,
   *"What are the most borrowed books this month?"*, *"Who borrowed the most
   last month?"* - by typing or by voice (mic button in the chat box). See
   §4b for configuring a real LLM instead of the built-in rule-based engine.

### 4a. Adding your own login

- Self-service: open **Register** on the login screen (creates a Member
  account + profile together).
- A librarian can provision another staff account: `POST /api/auth/librarians`
  (see the Postman collection's **Auth** folder) while signed in as an
  existing librarian.
- `Jwt:SigningKey` must be set in every environment (already set for
  Development/local/docker-compose with placeholder dev keys - **generate a
  real secret for any real deployment**, e.g. `openssl rand -base64 48`, and
  set it via the `Jwt__SigningKey` environment variable, never committed).

### 4b. Voice search & the chat assistant — configuration

Both features work with **zero external accounts** out of the box (Web
Speech API for voice, a rule-based engine for chat) and can optionally be
pointed at a real LLM/speech provider purely by configuration - no code
change, and a missing/invalid key never stops the app from starting, it just
makes that one feature answer with a clear "not configured" message.

Set these in `src/Library.Api/appsettings.Development.json` /
`appsettings.local.json` (never commit a real key to `appsettings.json`) or
as environment variables (the `__` double-underscore form, e.g.
`Chat__Anthropic__ApiKey`) — the same convention as `Database`/`Jwt`.

| Setting | Values | Default |
|---|---|---|
| `Chat:Enabled` | `true` / `false` | `true` |
| `Chat:Provider` | `RuleBased` \| `Anthropic` \| `OpenAI` | `RuleBased` |
| `Chat:Anthropic:ApiKey` | your key from [console.anthropic.com](https://console.anthropic.com/) (Settings → API Keys) | *(empty)* |
| `Chat:Anthropic:Model` | any Messages-API model id | `claude-3-5-haiku-20241022` |
| `Chat:OpenAI:ApiKey` | your key from [platform.openai.com](https://platform.openai.com/api-keys) | *(empty)* |
| `Chat:OpenAI:Model` | any Chat-Completions model id | `gpt-4o-mini` |
| `Speech:Enabled` | `true` / `false` | `true` |
| `Speech:Provider` | `WebSpeech` (client-side, no key needed) \| `HuggingFace` | `WebSpeech` |
| `Speech:HuggingFace:ApiKey` | your token from [huggingface.co/settings/tokens](https://huggingface.co/settings/tokens) | *(empty)* |
| `Speech:HuggingFace:Model` | any ASR model on the Inference API | `openai/whisper-large-v3` |

**Quick recipes:**

```bash
# Use Anthropic for chat (env vars, nothing committed):
export Chat__Provider=Anthropic
export Chat__Anthropic__ApiKey=sk-ant-...
dotnet run --project src/Library.Api

# Use OpenAI instead:
export Chat__Provider=OpenAI
export Chat__OpenAI__ApiKey=sk-...

# Use Hugging Face for server-side speech-to-text instead of the browser:
export Speech__Provider=HuggingFace
export Speech__HuggingFace__ApiKey=hf_...

# Turn the assistant off entirely:
export Chat__Enabled=false
```

Verify: sign in as the librarian, open the chat button, ask "How many copies
of Clean Code are available?" - the response's `provider` field
(`RuleBased`/`Anthropic`/`OpenAI`) confirms which engine answered.

---

## 5. Tests

```bash
dotnet test LibraryManagementSystem.slnx        # unit + integration (107)

cd frontend/library-web
npm run lint && npm run build && npm test       # frontend gate (11 Vitest tests)

# load / stress tests (separate console app, not run by `dotnet test`):
FeatureFlags__EnableRateLimiting=false dotnet run --project src/Library.Api &
dotnet run --project tests/Library.LoadTests -c Release
```

See `tests/DEVELOPERS-GUIDE.md` and `tests/Library.LoadTests/README.md`.

---

## 6. Observability

- **Traces** — set `FeatureFlags:EnableOpenTelemetry = true` (compose does this)
  and open Jaeger at http://localhost:16686; pick service `Library.Api`. Each
  request is a trace with child DB spans.
- **Structured file logs** — `logs/` under the API content root:
  - `runtime-error-logs/` — unhandled 500s + background-job faults
  - `build-error-logs/` — startup failures (incl. the diagnosed DB-down cause)
  - `query-logs/` — every SQL command (text, params by name/type, duration, provider)
  - `exception-logs/` — expected business exceptions mapped to 4xx
  Download a day's file via `GET /api/logs/available` then `/api/logs/download`.
- **Health** — `GET /health` reports the persistence provider and, if it can't
  connect, why.

---

## 7. Migrations & databases

Full reference: **`MIGRATIONS.md`** (repo root). The essentials, run from the
repo root:

```bash
# one-time
dotnet tool install --global dotnet-ef --version 10.0.11
docker compose up -d db                                    # local Postgres on :5433 (host-published; container-internal is still 5432)

# add a schema change
dotnet ef migrations add <Name> \
  --project src/Library.Infrastructure --startup-project src/Library.Api \
  --output-dir Persistence/Migrations

# apply it (Development also does this automatically on `dotnet run`)
dotnet ef database update \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# refresh the checked-in schema script
dotnet ef migrations script --idempotent --output docs/database/schema.sql \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# start over if the database is in a bad state
dotnet ef database drop -f \
  --project src/Library.Infrastructure --startup-project src/Library.Api
```

On startup the API brings the database to the current schema, **including the
case where the tables already exist but EF's `__EFMigrationsHistory` is empty**
(a DB created from `docs/database/schema.sql`, an old `EnsureCreated`, or a
restored dump) — it adopts the existing schema instead of failing with
`42P07 relation "books" already exists`.

SQL files in `docs/database/`: `schema.sql` (full schema, generated),
`seed-data.sql` (runnable demo rows), `er-diagram.md`.

Switch provider with `Database:Provider` (`Postgres` primary, also `SqlServer`,
`Sqlite`, `InMemory`).

---

## 8. Where to go next

- `docs/ROADMAP.md` — phase status and what's left
- `docs/ai-handover.md` — current execution state + exact next commands
- `docs/programmers-guide/` — how to add a CRUD, a cron job, a migration, etc.
- `postman/README.md` — the full API as a Postman collection (login once,
  every other request authenticates automatically); run the whole thing with
  `npx newman run postman/Library-Management-System.postman_collection.json -e postman/Library-Management-System.postman_environment.json`
