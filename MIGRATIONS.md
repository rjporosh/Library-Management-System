# Database Migrations

All commands run **from the repository root**. Migrations are authored against
**PostgreSQL** (the primary provider); other relational providers are created
on demand.

## Prerequisites

```bash
dotnet tool install --global dotnet-ef --version 10.0.11   # once
docker compose up -d db                                    # a local Postgres
```

The design-time connection string comes from `LMS_DESIGN_CONNECTION`, else
`Host=localhost;Port=5432;Database=library;Username=library;Password=library`.

## Everyday commands

```bash
# Add a migration
dotnet ef migrations add <Name> \
  --project src/Library.Infrastructure --startup-project src/Library.Api \
  --output-dir Persistence/Migrations

# Apply migrations to the configured database
dotnet ef database update \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# Roll back to a specific migration (use 0 to undo everything)
dotnet ef database update <PreviousMigrationName> \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# Remove the last (unapplied) migration
dotnet ef migrations remove \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# Generate an idempotent SQL script (for review / manual prod deploys)
dotnet ef migrations script --idempotent \
  --output docs/database/schema.sql \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# Check the model and migrations are in sync (CI does this)
dotnet ef migrations has-pending-model-changes \
  --project src/Library.Infrastructure --startup-project src/Library.Api
```

## Schema & seed SQL (checked in)

`docs/database/schema.sql` — idempotent full schema, generated from the
migrations. `docs/database/seed-data.sql` — runnable demo/QA rows (books,
copies, members, one active + one overdue borrow), safe to re-run.

```bash
# Regenerate the schema script after adding a migration
dotnet ef migrations script --idempotent --output docs/database/schema.sql \
  --project src/Library.Infrastructure --startup-project src/Library.Api

# Apply schema + seed to a fresh database (no .NET needed)
psql "$LMS_DESIGN_CONNECTION" -f docs/database/schema.sql
psql "$LMS_DESIGN_CONNECTION" -f docs/database/seed-data.sql

# ...or via the docker-compose Postgres
docker compose exec -T db psql -U library -d library < docs/database/schema.sql
docker compose exec -T db psql -U library -d library < docs/database/seed-data.sql
```

## TL;DR for AI agents

```
# add migration + refresh schema.sql + verify, from repo root:
dotnet ef migrations add <Name> --project src/Library.Infrastructure --startup-project src/Library.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --output docs/database/schema.sql --project src/Library.Infrastructure --startup-project src/Library.Api
dotnet ef database update --project src/Library.Infrastructure --startup-project src/Library.Api
dotnet build LibraryManagementSystem.slnx    # expect 0/0
# no DB handy? demo without migrations:
Database__Provider=InMemory dotnet run --project src/Library.Api
```

## Running the API against a database

- **Development** (`appsettings.Development.json`) uses Postgres with
  `MigrateOnStartup: true` — `dotnet run --project src/Library.Api` applies
  migrations and seeds automatically. It needs `docker compose up -d db`.
- **No database?** Override the provider:
  `Database__Provider=InMemory dotnet run --project src/Library.Api`
- **Production** (`appsettings.Production.json`) uses Postgres with
  `MigrateOnStartup: false`; apply migrations as a separate deploy step.

## Switching providers

Set `Database:Provider` in the relevant `appsettings*.json` (or the
`Database__Provider` env var):

| Provider    | Status                                               |
|-------------|------------------------------------------------------|
| `Postgres`  | primary, migrations committed                        |
| `SqlServer` | supported; run `dotnet ef database update` to create |
| `Sqlite`    | supported (used for fast tests)                      |
| `InMemory`  | zero-dependency demo/test mode, no migrations        |
| `MySql` / `Oracle` / `Access` / `Mongo` | acknowledged config slots; throw `NotSupportedException` (no EF Core 10 driver / not relational) |

If the database is unreachable at startup the API writes a diagnosed cause and
fix to `logs/build-error-logs/` and exits — check there first.
