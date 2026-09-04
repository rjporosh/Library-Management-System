# Architecture & Layering

```
Library.Api            ASP.NET Core controllers, middleware, background jobs,
  │                    composition root (Program.cs). References Application + Infrastructure.
  ├── Library.Application   Use-case services, DTOs/models, persistence
  │     │                   abstractions (interfaces), cross-cutting helpers
  │     │                   (Result, ErrorCodes, validators, search builder).
  │     │                   References Domain only. No EF Core types.
  │     └── Library.Domain  Entities + enums. No dependencies.
  └── Library.Infrastructure   EF Core (LibraryDbContext, provider factory,
                               repositories, migrations), the in-memory
                               repositories, ClosedXML, file logging.
                               References Application + Domain.
```

## Conventions

- **Feature folders**, not CQRS: `Features/<Feature>/<Feature>Service.cs` +
  `Models/`. Services are `sealed`, use primary-constructor DI, and are
  registered `Scoped` in `ApplicationServiceExtensions`.
- **Persistence is an interface in Application** (`Abstractions/Persistence`),
  implemented twice in Infrastructure (`Repositories/EfCore`,
  `Repositories/InMemory`). `IUnitOfWork` is the commit boundary.
- **Errors**: new features return `Result` / `Result<T>` carrying `ApiError`s;
  the API maps them to the standard `ApiErrorResponse` envelope. Older code
  throws exceptions caught by `GlobalExceptionHandlingMiddleware`.
- **Options POCOs** (`ObservabilitySettings`, `DatabaseOptions`,
  `BulkImportOptions`) are bound once in `Program.cs` and shared as singletons —
  no `IOptions<T>`.
- **Enums** are serialised as their string name everywhere (JSON and DB).

## Request flow

`Controller` → `Service` (validates, calls repositories, `IUnitOfWork.SaveChanges`)
→ `Repository` (EF or in-memory) → `Result`/DTO back → `ToActionResult` maps to HTTP.
