# ADR 0001 — Persistence & provider strategy

**Status:** Accepted

## Context
The spec requires a relational database with migrations, plus the ability to
switch RDBMS by configuration only. The MVP shipped on in-memory repositories.

## Decision
- EF Core is the persistence engine. **PostgreSQL is the primary provider**;
  `Database:Provider` selects `InMemory` (default, zero-dependency demo/test),
  `Postgres`, `SqlServer` or `Sqlite` with no code change.
- A `DatabaseProviderConfigurator` maps the option to `UseNpgsql` / `UseSqlServer`
  / `UseSqlite`. Adding a provider is one `case` + a NuGet reference.
- **Migrations are authored for PostgreSQL only.** Per-provider migration
  folders are a permanent maintenance tax for a demo-supported matrix; other
  relational providers create the schema on demand (`dotnet ef database update`
  with a provider override, or `EnsureCreated` in tests).
- MySQL / Oracle are acknowledged config slots that throw `NotSupportedException`
  with a clear message — Pomelo 9 needs EF Core 9, and there is no EF Core 10
  Oracle provider yet. MS Access / MongoDB are not relational and are out of
  scope for the EF model.
- Persistence interfaces live in Application; `IUnitOfWork` / `ITransaction`
  abstract the commit boundary so services are identical in-memory and under EF.

## Consequences
- One database is migration-managed; the rest are documented.
- The in-memory provider stays a first-class mode for demos and fast tests.
- A future provider only needs the configurator case and a driver package.
