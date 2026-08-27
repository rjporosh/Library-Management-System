using Library.Domain.Entities;
using Library.Infrastructure.Persistence.Repositories.InMemory;

namespace Library.Infrastructure.Persistence.Repositories.InMemory.Seed;

public sealed class InMemoryDataSeeder(
    InMemoryBookRepository bookRepository,
    InMemoryBookCopyRepository bookCopyRepository,
    InMemoryMemberRepository memberRepository)
{
    public void Seed()
    {
        var cleanCode = new Book(
            Guid.NewGuid(),
            "9780132350884",
            "Clean Code",
            "Robert C. Martin",
            2008,
            "A handbook of agile software craftsmanship.");

        var pragmaticProgrammer = new Book(
            Guid.NewGuid(),
            "9780135957059",
            "The Pragmatic Programmer",
            "David Thomas & Andrew Hunt",
            2019,
            "Your journey to mastery.");

        var domainDrivenDesign = new Book(
            Guid.NewGuid(),
            "9780321125217",
            "Domain-Driven Design",
            "Eric Evans",
            2003,
            "Tackling complexity in the heart of software.");

        bookRepository.Seed(
        [
            cleanCode,
            pragmaticProgrammer,
            domainDrivenDesign
        ]);

        bookCopyRepository.Seed(
        [
            new BookCopy(Guid.NewGuid(), cleanCode.Id, "BC-0001"),
            new BookCopy(Guid.NewGuid(), cleanCode.Id, "BC-0002"),
            new BookCopy(Guid.NewGuid(), pragmaticProgrammer.Id, "BC-0003"),
            new BookCopy(Guid.NewGuid(), pragmaticProgrammer.Id, "BC-0004"),
            new BookCopy(Guid.NewGuid(), domainDrivenDesign.Id, "BC-0005")
        ]);

        memberRepository.Seed(
        [
            new Member(
                Guid.NewGuid(),
                "MEM-100001",
                "Alice Johnson",
                "alice@example.com"),

            new Member(
                Guid.NewGuid(),
                "MEM-100002",
                "Bob Smith",
                "bob@example.com"),

            new Member(
                Guid.NewGuid(),
                "MEM-100003",
                "Charlie Brown",
                "charlie@example.com")
        ]);
    }
}
