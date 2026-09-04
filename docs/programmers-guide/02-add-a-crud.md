# Add a CRUD Feature

Worked example: adding a `Publisher` entity.

1. **Domain** — `src/Library.Domain/Entities/Publisher.cs`: `sealed class`,
   `Guid Id { get; init; }`, `private set` properties, a public constructor, a
   `private Publisher() { }` for EF, and behaviour methods (no anaemic setters).
   Add any enum under `Enums/` (append values, never reorder).

2. **Persistence abstraction** — `src/Library.Application/Abstractions/Persistence/IPublisherRepository.cs`:
   `IQueryable<Publisher> Query()`, `GetByIdAsync`, `AddAsync`, `AddRangeAsync`,
   `UpdateAsync`, `DeleteAsync`, `ExistsBy…Async` for uniqueness.

3. **EF config** — add a `DbSet<Publisher>` + configuration block to
   `LibraryDbContext.OnModelCreating` (max lengths, unique indexes, `AddAudit`).
   Then `dotnet ef migrations add AddPublisher …` (see `MIGRATIONS.md`).

4. **Repositories** — implement `IPublisherRepository` in
   `Repositories/EfCore/EfRepositories.cs` **and** a new
   `Repositories/InMemory/InMemoryPublisherRepository.cs`. Register both in
   `InfrastructureServiceExtensions` (`AddEfCore` / `AddInMemory`).

5. **Validation** — add a `PublisherCandidate` + `PublisherValidator` in
   `Common/Validation/EntityValidators.cs`; add stable codes to `ErrorCodes`.

6. **Search map** — `Features/Publishers/PublisherSearchMap.cs`:
   `new SearchFieldMap<Publisher>().Field("name", p => p.Name, quickSearch: true)…`

7. **Service** — `Features/Publishers/PublisherService.cs`: `Search`,
   `GetByIdAsync`, `CreateAsync`/`UpdateAsync`/`DeleteAsync` returning
   `Result<PublisherResponse>` / `Result`; call
   `unitOfWork.SaveChangesAsync` after every mutation. Register `Scoped` in
   `ApplicationServiceExtensions`.

8. **Controller** — `Controllers/PublishersController.cs`: `[HttpPost("search")]`,
   `[HttpGet]` list, `GetById`, `Create`, `Update`, `Delete`. Return
   `result.ToActionResult(this)` / `ToCreatedResult(...)`. Add XML docs +
   `ProducesResponseType`.

9. **Tests** — unit tests for the service (use the real `InMemory…Repository`
   from `Library.Infrastructure` + `NoOpUnitOfWork`); an integration test class
   using `LibraryApiFactory`.

10. **Frontend** — a typed client in `src/api/index.ts`, a `FieldDef[]`, and a
    page built from `DataTable` + `AdvancedSearch` + `Modal` (copy `BooksPage`).
