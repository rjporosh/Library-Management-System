# Persistence & Database Providers

## Configuration (`appsettings*.json` → `Database`)

| Key | Values | Notes |
|---|---|---|
| `Provider` | `InMemory` (default) `Postgres` `SqlServer` `Sqlite` `MySql` `Oracle` `Access` `Mongo` | primary is `Postgres`; `MySql`/`Oracle`/`Access`/`Mongo` throw `NotSupportedException` (no EF Core 10 driver / not relational) |
| `Orm` | `EfCore` (default) `Dapper` | read-path ORM; `Dapper` slot reserved |
| `ConnectionString` | provider connection string | required for any relational provider |
| `MigrateOnStartup` | `bool` | apply pending migrations when the API starts |
| `SeedOnStartup` | `bool` | seed demo data when the DB is empty |

Bound once in `Program.cs` to `DatabaseOptions`, shared as a singleton.

## How it wires up

- `InfrastructureServiceExtensions.AddInfrastructure` branches on
  `DatabaseOptions.IsRelational`:
  - relational → `AddDbContext<LibraryDbContext>` (provider chosen by
    `DatabaseProviderConfigurator.Configure`) + `Ef*Repository` (scoped) +
    `EfUnitOfWork` + `QueryLoggingInterceptor`.
  - in-memory → the hand-rolled `InMemory*Repository` singletons +
    `NoOpUnitOfWork`.
- Services depend only on the interfaces + `IUnitOfWork`, so they are identical
  in both modes.

## The model

`LibraryDbContext` — enums as `string` columns, unique indexes (ISBN, Barcode,
MembershipNumber, Email), FK `OnDelete Restrict`, composite indexes for the
one-active-borrow and overdue scans, shadow `CreatedAtUtc`/`UpdatedAtUtc`
maintained in `SaveChangesAsync`.

## Adding a provider

Add a NuGet reference and one `case` in `DatabaseProviderConfigurator`. Nothing
else changes.

## DB-down behaviour

A startup connection failure is classified (server unreachable / database
missing / auth failed / schema stale) and written to `logs/build-error-logs/`
with the provider, host, database and a fix hint, then rethrown. At runtime,
`/health` reports the provider and the failure reason.
